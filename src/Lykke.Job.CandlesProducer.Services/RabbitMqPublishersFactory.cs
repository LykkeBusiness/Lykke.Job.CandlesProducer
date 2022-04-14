// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Publisher.Strategies;
using Microsoft.Extensions.Logging;

namespace Lykke.Job.CandlesProducer.Services
{
    [UsedImplicitly]
    public class RabbitMqPublishersFactory : IRabbitMqPublishersFactory
    {
        private readonly ILog _log;
        private readonly ILoggerFactory _loggerFactory;

        public RabbitMqPublishersFactory(ILog log, ILoggerFactory loggerFactory)
        {
            _log = log;
            _loggerFactory = loggerFactory;
        }

        public RabbitMqPublisher<TMessage> Create<TMessage>(
            IRabbitMqSerializer<TMessage> serializer, 
            string connectionString, 
            string @namespace, 
            string endpoint)
        {
            try
            {
                var settings = RabbitMqSubscriptionSettings
                    .CreateForPublisher(connectionString, @namespace, endpoint)
                    .MakeDurable();

                var result = new RabbitMqPublisher<TMessage>(_loggerFactory, settings)
                    .SetSerializer(serializer)
                    .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                    .PublishSynchronously();
                result.Start();
                return result;
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync(nameof(RabbitMqPublishersFactory), nameof(Create), null, ex).Wait();
                throw;
            }
        }
    }
}
