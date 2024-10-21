// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using Lykke.Job.CandlesProducer.Services.Quotes;
using Lykke.Job.CandlesProducer.Services.Quotes.Mt;
using Lykke.Job.CandlesProducer.Services.Quotes.Spot;
using Lykke.Job.CandlesProducer.Services.Trades.Mt;
using Lykke.Job.CandlesProducer.Services.Trades.Spot;
using Microsoft.Extensions.DependencyInjection;
using Lykke.Job.CandlesProducer.Services.Trades.Spot.Messages;
using Lykke.Job.CandlesProducer.Services.Trades.Mt.Messages;
using Lykke.Job.QuotesProducer.Contract;
using Lykke.Job.CandlesProducer.Services.Quotes.Mt.Messages;
using Lykke.Job.CandlesProducer.Services.Trades;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Microsoft.Extensions.Logging;
using Lykke.RabbitMqBroker.Subscriber.MessageReadStrategies;

namespace Lykke.Job.CandlesProducer.Modules
{
    public static class RabbitMqRegistrationExtentions
    {
        public static IServiceCollection AddQuotesListener(
            this IServiceCollection services,
            QuotesSourceType quotesSourceType,
            RabbitMqSubscriptionSettings settings)
        {
            switch (quotesSourceType)
            {
                case QuotesSourceType.Spot:
                    services.AddListener<QuoteMessage, SpotQuotesHandler>(settings, RabbitMqListenerOptions<QuoteMessage>.Json.NoLoss);
                    break;
                case QuotesSourceType.Mt:
                    services.AddListener<MtQuoteMessage, MtQuotesHandler>(settings, RabbitMqListenerOptions<MtQuoteMessage>.Json.NoLoss);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(message: "Unknown quotes source type", null);
            };

            return services;
        }

        public static IServiceCollection AddTradesListener(
            this IServiceCollection services,
            QuotesSourceType quotesSourceType,
            RabbitMqSubscriptionSettings settings)
        {
            switch (quotesSourceType)
            {
                case QuotesSourceType.Spot:
                    services.AddListener<LimitOrdersMessage, SpotTradesHandler>(settings, RabbitMqListenerOptions<LimitOrdersMessage>.Json.NoLoss);
                    break;
                case QuotesSourceType.Mt:
                    services.AddListener<MtTradeMessage, MtTradesHandler>(settings, RabbitMqListenerOptions<MtTradeMessage>.Json.NoLoss);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(message: "Unknown quotes source type", null);
            };

            return services;
        }

        public static IServiceCollection AddQuotesPoisonHandler(
            this IServiceCollection services,
            RabbitMqSubscriptionSettings settings)
        {
            return services.AddSingleton<IQuotesPoisonHandlingService>(p =>
                new QuotesPoisonHandlingService(
                    new ParallelExecutionGuardPoisonQueueDecorator(
                        new PoisonQueueHandler(
                            settings.ConnectionString,
                            p.GetRequiredService<IConnectionProvider>(),
                            PoisonQueueConsumerConfigurationOptions.Create(
                                poisonQueueName: PoisonQueueName.Create(settings.QueueName),
                                exchangeName: ExchangeName.Create(settings.ExchangeName),
                                routingKey: RoutingKey.Create(settings.RoutingKey)
                            ),
                            p.GetRequiredService<ILoggerFactory>()))));
        }

        public static IServiceCollection AddTradesPoisonHandler(
            this IServiceCollection services,
            RabbitMqSubscriptionSettings settings)
        {
            return services.AddSingleton<ITradesPoisonHandlingService>(p =>
                new TradesPoisonHandlingService(
                    new ParallelExecutionGuardPoisonQueueDecorator(
                        new PoisonQueueHandler(
                            settings.ConnectionString,
                            p.GetRequiredService<IConnectionProvider>(),
                            PoisonQueueConsumerConfigurationOptions.Create(
                                poisonQueueName: PoisonQueueName.Create(settings.QueueName),
                                exchangeName: ExchangeName.Create(settings.ExchangeName),
                                routingKey: RoutingKey.Create(settings.RoutingKey)
                            ),
                            p.GetRequiredService<ILoggerFactory>()))));
        }

        private static void ConfigureMiddlewaresCallback<T>(RabbitMqSubscriber<T> subscriber, IServiceProvider provider)
        {
            var loggerFactory = provider.GetService<ILoggerFactory>();
            subscriber
                .UseMiddleware(
                    new DeadQueueMiddleware<T>(
                        logger: loggerFactory.CreateLogger<DeadQueueMiddleware<T>>()))
                .UseMiddleware(
                    new ResilientErrorHandlingMiddleware<T>(
                        logger: loggerFactory.CreateLogger<ResilientErrorHandlingMiddleware<T>>(),
                        retryTimeout: TimeSpan.FromSeconds(10),
                        retryNum: 10));
        }

        private static IServiceCollection AddListener<TMessage, THandler>(
            this IServiceCollection services,
            RabbitMqSubscriptionSettings settings,
            RabbitMqListenerOptions<TMessage> options)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>
        {
            services.AddRabbitMqListener<TMessage, THandler>(
                settings,
                ConfigureMiddlewaresCallback)
            .AddOptions(options);

            return services;
        }
    }
}
