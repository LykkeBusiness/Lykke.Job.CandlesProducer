// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using Lykke.RabbitMqBroker;

namespace Lykke.Job.CandlesProducer.Core.Services.Candles
{
    public interface ICandlesPublisher : IStartStop
    {
        string ShardName { get; }
        Task PublishAsync(IReadOnlyCollection<CandleUpdateResult> updates);
        bool CanPublish(string assetPairId);
    }
}
