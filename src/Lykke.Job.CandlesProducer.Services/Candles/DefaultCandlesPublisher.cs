// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;

namespace Lykke.Job.CandlesProducer.Services.Candles
{
    public class DefaultCandlesPublisher : CandlesPublisher, IDefaultCandlesPublisher
    {
        public DefaultCandlesPublisher(IRabbitMqPublishersFactory publishersFactory, 
            string connectionString,
            string nspace, 
            string shardName) 
            : base(publishersFactory, connectionString, nspace, shardName, string.Empty)
        {
        }

        public override bool CanPublish(string assetPairId) => true;
    }
}
