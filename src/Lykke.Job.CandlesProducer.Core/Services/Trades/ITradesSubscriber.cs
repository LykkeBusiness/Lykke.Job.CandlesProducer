// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.RabbitMqBroker;

namespace Lykke.Job.CandlesProducer.Core.Services.Trades
{
    public interface ITradesSubscriber : IStartStop
    {
        RabbitMqSubscriptionSettings SubscriptionSettings { get; }
    }
}
