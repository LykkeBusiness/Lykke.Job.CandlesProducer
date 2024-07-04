// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Autofac;
using CorporateActions.Broker.Contracts.Workflow;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.Configuration.BoundedContext;
using Lykke.Cqrs.Configuration.Routing;
using Lykke.Cqrs.Middleware.Logging;
using Lykke.Job.CandlesProducer.Settings;
using Lykke.Job.CandlesProducer.Workflow;
using Lykke.Messaging.Serialization;
using Lykke.Snow.Common.Correlation.Cqrs;
using Lykke.Snow.Cqrs;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Lykke.Job.CandlesProducer.Modules
{
    public class CqrsModule : Module
    {
        private readonly CqrsSettings _settings;
        private readonly CqrsContextNamesSettings _contextNames;

        private const string DefaultRoute = "self";
        private const string DefaultPipeline = "commands";
        private const string DefaultEventPipeline = "events";

        public CqrsModule(CqrsSettings settings)
        {
            _settings = settings;
            _contextNames = settings.ContextNames;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context))
                .As<IDependencyResolver>()
                .SingleInstance();

            // Sagas & command handlers
            builder.RegisterAssemblyTypes(GetType().Assembly).Where(t =>
                    new[] { "Saga", "CommandsHandler", "Projection" }
                        .Any(ending => t.Name.EndsWith(ending)))
                .AsSelf();

            builder.Register(CreateEngine)
                .As<ICqrsEngine>()
                .SingleInstance();
        }
        
        private CqrsEngine CreateEngine(IComponentContext ctx)
        {
            var rabbitMqConventionEndpointResolver = new RabbitMqConventionEndpointResolver("RabbitMq",
                SerializationFormat.MessagePack,
                environment: _settings.EnvironmentName);

            var rabbitMqSettings = new ConnectionFactory
            {
                Uri = new Uri(_settings.ConnectionString, UriKind.Absolute)
            };
            
            var loggerFactory = ctx.Resolve<ILoggerFactory>();

            var engine = new RabbitMqCqrsEngine(
                loggerFactory,
                ctx.Resolve<IDependencyResolver>(),
                new DefaultEndpointProvider(),
                rabbitMqSettings.Endpoint.ToString(),
                rabbitMqSettings.UserName,
                rabbitMqSettings.Password,
                true,
                Register.DefaultEndpointResolver(rabbitMqConventionEndpointResolver),
                Register.CommandInterceptors(new DefaultCommandLoggingInterceptor(loggerFactory)),
                Register.EventInterceptors(new DefaultEventLoggingInterceptor(loggerFactory)),
                RegisterContext());
            var correlationManager = ctx.Resolve<CqrsCorrelationManager>();
            engine.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
            engine.SetWriteHeadersFunc(correlationManager.BuildCorrelationHeadersIfExists);

            return engine;
        }
        
        private IRegistration RegisterContext()
        {
            var contextRegistration = Register.BoundedContext(_contextNames.CorporateActionsBroker)
                .FailedCommandRetryDelay((long)_settings.RetryDelay.TotalMilliseconds)
                .ProcessingOptions(DefaultRoute)
                .MultiThreaded(_settings.CommandsHandlersThreadCount)
                .QueueCapacity(_settings.CommandsHandlersQueueCapacity);
            RegisterRFactorCommandsHandler(contextRegistration);
            return contextRegistration;
        }
        
        private void RegisterRFactorCommandsHandler(ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration
                .ListeningCommands(
                    typeof(UpdateCurrentCandlesCommand)
                )
                .On(DefaultPipeline)
                .WithCommandsHandler<RFactorCommandsHandler>()
                .PublishingEvents(
                    typeof(CurrentCandlesUpdatedEvent)
                )
                .With(DefaultEventPipeline);
        }
    }
}
