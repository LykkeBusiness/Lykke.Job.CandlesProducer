// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.RabbitMqBroker.Subscriber.Deserializers;
using Lykke.RabbitMqBroker.Subscriber.MessageReadStrategies;
using Lykke.RabbitMqBroker.Subscriber.Middleware.ErrorHandling;
using Microsoft.Extensions.Logging;

namespace Lykke.Job.CandlesProducer.Services
{
    [UsedImplicitly]
    public class RabbitMqSubscribersFactory : IRabbitMqSubscribersFactory
    {
        private readonly ILog _log;
        private readonly ILoggerFactory _loggerFactory;

        public RabbitMqSubscribersFactory(ILog log, ILoggerFactory loggerFactory)
        {
            _log = log;
            _loggerFactory = loggerFactory;
        }

        public IStartStop Create<TMessage>(RabbitMqSubscriptionSettings settings, Func<TMessage, Task> handler)
        {
            return new RabbitMqSubscriber<TMessage>(
                    _loggerFactory.CreateLogger<RabbitMqSubscriber<TMessage>>(),
                    settings)
                .SetMessageDeserializer(new JsonMessageDeserializer<TMessage>())
                .SetMessageReadStrategy(new MessageReadQueueStrategy())
                .UseMiddleware(new DeadQueueMiddleware<TMessage>(
                    _loggerFactory.CreateLogger<DeadQueueMiddleware<TMessage>>()))
                .UseMiddleware(new ResilientErrorHandlingMiddleware<TMessage>(
                    _loggerFactory.CreateLogger<ResilientErrorHandlingMiddleware<TMessage>>(),
                    TimeSpan.FromSeconds(10),
                    10))
                .Subscribe(handler)
                .CreateDefaultBinding()
                .Start();
        }
    }
}
