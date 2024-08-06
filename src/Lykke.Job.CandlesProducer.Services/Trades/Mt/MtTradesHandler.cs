// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Core.Domain.Trades;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Services.Trades.Mt.Messages;
using Lykke.RabbitMqBroker.Subscriber;
using Microsoft.Extensions.Logging;

namespace Lykke.Job.CandlesProducer.Services.Trades.Mt
{
    [UsedImplicitly]
    public class MtTradesHandler : IMessageHandler<MtTradeMessage>
    {
        private readonly ICandlesManager _candlesManager;
        private readonly ILogger<MtTradesHandler> _logger;

        public MtTradesHandler(
            ICandlesManager candlesManager,
            ILogger<MtTradesHandler> logger)
        {
            _logger = logger;
            _candlesManager = candlesManager;
        }

        public async Task Handle(MtTradeMessage message)
        {
            if (message.Price <= 0 ||
                message.Volume <= 0)
            {
                _logger.LogWarning("Got an MT trade with non-positive price or volume value: {MessageJson}",
                    message.ToJson());
                return;
            }

            var quotingVolume = (double)(message.Volume * message.Price);

            var trade = new Trade(
                message.AssetPairId,
                message.Date,
                (double)message.Volume,
                quotingVolume,
                (double)message.Price);

            await _candlesManager.ProcessTradeAsync(trade);
        }
    }
}
