// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;

namespace Lykke.Job.CandlesProducer.Core.Services
{
    public interface IRabbitMqPublishersFactory
    {
        RabbitMqPublisher<TMessage> Create<TMessage>(IRabbitMqSerializer<TMessage> serializer, string connectionString, string @namespace, string endpoint);
    }
}
