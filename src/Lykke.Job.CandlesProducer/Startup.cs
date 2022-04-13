// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.HttpClientGenerator.Infrastructure;
using Lykke.Job.CandlesProducer.Core.Domain;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Models;
using Lykke.Job.CandlesProducer.Modules;
using Lykke.Job.CandlesProducer.Services.Assets;
using Lykke.Job.CandlesProducer.Settings;
using Lykke.Logs;
using Lykke.Logs.MsSql;
using Lykke.Logs.MsSql.Repositories;
using Lykke.Logs.Serilog;
using Lykke.Logs.Slack;
using Lykke.SettingsReader;
using Lykke.SlackNotification.AzureQueue;
using Lykke.Snow.Common.Startup.Hosting;
using Lykke.Snow.Common.Startup.Log;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.Candles;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Lykke.Job.CandlesProducer
{
    [UsedImplicitly]
    public class Startup
    {
        private IReloadingManager<AppSettings> _mtSettingsManager;
        
        private CandlesProducerSettingsContract _candlesProducerSettings;
        private IHostingEnvironment Environment { get; set; }
        private ILifetimeScope ApplicationContainer { get; set; }
        private IConfigurationRoot Configuration { get; }
        private ILog Log { get; set; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddSerilogJson(env)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            Environment = env;
        }

        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver =
                        new Newtonsoft.Json.Serialization.DefaultContractResolver();
                });

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration("v1", "CandlesProducer API");
            });
            
            LoadConfiguration();
            
            Log = CreateLogWithSlack(
                Configuration,
                services,
                _mtSettingsManager);

            services.AddSingleton<ILoggerFactory>(x => new WebHostLoggerFactory(Log));

            services.AddApplicationInsightsTelemetry();
        }

        private void LoadConfiguration()
        {
            // load service settings
            _mtSettingsManager = Configuration.LoadSettings<AppSettings>();

            // load candles sharding settings from settings service
            var candlesSettingsClientBuilder = HttpClientGenerator.HttpClientGenerator
                .BuildForUrl(_mtSettingsManager.CurrentValue.Assets.ServiceUrl)
                .WithAdditionalCallsWrapper(new ExceptionHandlerCallsWrapper());

            if (!string.IsNullOrWhiteSpace(_mtSettingsManager.CurrentValue.Assets.ApiKey))
            {
                candlesSettingsClientBuilder =
                    candlesSettingsClientBuilder.WithApiKey(_mtSettingsManager.CurrentValue.Assets.ApiKey);
            }

            var candlesSettingsClient = candlesSettingsClientBuilder.Create().Generate<ICandlesSettingsApi>();

            _candlesProducerSettings = candlesSettingsClient.GetProducerSettingsAsync().GetAwaiter().GetResult();
        }

        [UsedImplicitly]
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var quotesSourceType = _mtSettingsManager.CurrentValue.CandlesProducerJob != null
                ? QuotesSourceType.Spot
                : QuotesSourceType.Mt;
            
            var jobSettings = quotesSourceType == QuotesSourceType.Spot 
                ? _mtSettingsManager.Nested(x => x.CandlesProducerJob) 
                : _mtSettingsManager.Nested(x => x.MtCandlesProducerJob);
            
            builder.RegisterModule(new JobModule(
                jobSettings.CurrentValue, 
                jobSettings.Nested(x => x.Db), 
                _mtSettingsManager.CurrentValue.Assets,
                quotesSourceType, 
                Log));
            
            builder.RegisterModule(new CandlePublishersModule(
                jobSettings.CurrentValue.Rabbit.CandlesPublication, 
                _candlesProducerSettings));

            if (quotesSourceType == QuotesSourceType.Mt)
            {
                builder.RegisterBuildCallback(c => c.Resolve<MtAssetPairsManager>());
            }
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            ApplicationContainer = app.ApplicationServices.GetAutofacRoot();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseLykkeMiddleware("CandlesProducer", ex => new ErrorResponse { ErrorMessage = "Technical problem" });

            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swagger, httpReq) => 
                    swagger.Servers =
                        new List<OpenApiServer>
                        {
                            new OpenApiServer
                            {
                                Url = $"{httpReq.Scheme}://{httpReq.Host.Value}"
                            }
                        });
            });
            app.UseSwaggerUI(x =>
            {
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });
            app.UseStaticFiles();

            appLifetime.ApplicationStarted.Register(() => StartApplication().Wait());
            appLifetime.ApplicationStopping.Register(() => StopApplication().Wait());
            appLifetime.ApplicationStopped.Register(() => CleanUp().Wait());
        }

        private async Task StartApplication()
        {
            try
            {
                var startupManager = ApplicationContainer.Resolve<IStartupManager>();

                await startupManager.StartAsync();

                Program.AppHost.WriteLogs(Environment, Log);

                await Log.WriteMonitorAsync("", "", "Started");
            }
            catch (Exception ex)
            {
                await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
            }
        }

        private async Task StopApplication()
        {
            try
            {
                var shutdownManager = ApplicationContainer.Resolve<IShutdownManager>();

                await shutdownManager.ShutdownAsync();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StopApplication), "", ex);
                }
            }
        }

        private async Task CleanUp()
        {
            try
            {
                if (Log != null)
                {
                    await Log.WriteMonitorAsync("", "", "Terminating");
                }

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
                    (Log as IDisposable)?.Dispose();
                }
            }
        }

        private static ILog CreateLogWithSlack(IConfiguration configuration, IServiceCollection services, 
            IReloadingManager<AppSettings> settings)
        {
            var tableName = "CandlesProducerServiceLog";
            var consoleLogger = new LogToConsole();
            var aggregateLogger = new AggregateLogger();

            aggregateLogger.AddLog(consoleLogger);

            LykkeLogToAzureSlackNotificationsManager slackNotificationsManager = null;
            if (settings.CurrentValue.SlackNotifications != null)
            {
                // Creating slack notification service, which logs own azure queue processing messages to aggregate log
                var slackService = services.UseSlackNotificationsSenderViaAzureQueue(new AzureQueueIntegration.AzureQueueSettings
                {
                    ConnectionString = settings.CurrentValue.SlackNotifications.AzureQueue.ConnectionString,
                    QueueName = settings.CurrentValue.SlackNotifications.AzureQueue.QueueName
                }, aggregateLogger);

                slackNotificationsManager = new LykkeLogToAzureSlackNotificationsManager(slackService, consoleLogger);
                var logToSlack = LykkeLogToSlack.Create(slackService, "Prices");

                aggregateLogger.AddLog(logToSlack);
            }

            if (settings.CurrentValue.MtCandlesProducerJob.UseSerilog)
            {
                aggregateLogger.AddLog(new SerilogLogger(typeof(Startup).Assembly, configuration));
            }
            else if (settings.CurrentValue.MtCandlesProducerJob.Db.StorageMode == StorageMode.SqlServer)
            {
                aggregateLogger.AddLog(
                    new LogToSql(new SqlLogRepository(tableName, 
                        settings.CurrentValue.MtCandlesProducerJob.Db.LogsConnString)));
            }
            else if (settings.CurrentValue.MtCandlesProducerJob.Db.StorageMode == StorageMode.Azure)
            {
                var dbLogConnectionString = settings.CurrentValue.MtCandlesProducerJob.Db.LogsConnString;

                // Creating azure storage logger, which logs own messages to console log
                if (!string.IsNullOrEmpty(dbLogConnectionString) && !(dbLogConnectionString.StartsWith("${") 
                                                                      && dbLogConnectionString.EndsWith("}")))
                {
                    var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                        AzureTableStorage<Logs.LogEntity>.Create(settings.Nested(x => 
                            x.MtCandlesProducerJob.Db.LogsConnString), tableName, consoleLogger),
                        consoleLogger);

                    var azureStorageLogger = new LykkeLogToAzureStorage(
                        persistenceManager,
                        slackNotificationsManager,
                        consoleLogger);

                    azureStorageLogger.Start();

                    aggregateLogger.AddLog(azureStorageLogger);
                }
            }

            return aggregateLogger;
        }
    }
}
