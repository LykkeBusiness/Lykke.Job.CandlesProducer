// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Lykke.Job.CandlesProducer
{
    [UsedImplicitly]
    class Program
    {
        internal static IHost AppHost { get; private set; }

        public static string EnvInfo => Environment.GetEnvironmentVariable("ENV_INFO");

        static async Task Main()
        {
            Console.WriteLine($"CandlesProducer version {Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion}");
#if DEBUG
            Console.WriteLine("Is DEBUG");
#else
            Console.WriteLine("Is RELEASE");
#endif
            Console.WriteLine($"ENV_INFO: {EnvInfo}");

            try
            {
                AppHost = Host.CreateDefaultBuilder()
                    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureKestrel(serverOptions =>
                            {
                                // Set properties and call methods on options
                            })
                            .UseUrls("http://*:5000")
                            .UseContentRoot(Directory.GetCurrentDirectory())
                            .UseStartup<Startup>();
                    })
                    .Build();

                await AppHost.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error:");
                Console.WriteLine(ex);

                // Lets devops to see startup error in console between restarts in the Kubernetes
                var delay = TimeSpan.FromMinutes(1);

                Console.WriteLine();
                Console.WriteLine($"Process will be terminated in {delay}. Press any key to terminate immediately.");
                
                await Task.WhenAny(
                    Task.Delay(delay),
                    Task.Run(() =>
                    {
                        Console.ReadKey(true);
                    }));
            }

            Console.WriteLine("Terminated");
        }
    }
}
