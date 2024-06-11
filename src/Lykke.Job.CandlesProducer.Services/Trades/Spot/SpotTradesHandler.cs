// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Common;

using JetBrains.Annotations;

using Lykke.Job.CandlesProducer.Core.Domain.Trades;
using Lykke.Job.CandlesProducer.Core.Services.Assets;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Services.Trades.Spot.Messages;
using Lykke.RabbitMqBroker.Subscriber;

using Microsoft.Extensions.Logging;

namespace Lykke.Job.CandlesProducer.Services.Trades.Spot
{
    [UsedImplicitly]
    public class SpotTradesHandler : IMessageHandler<LimitOrdersMessage>
    {
        private readonly ICandlesManager _candlesManager;
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly ILogger<SpotTradesHandler> _logger;

        public SpotTradesHandler(
            ICandlesManager candlesManager,
            IAssetPairsManager assetPairsManager,
            ILogger<SpotTradesHandler> logger)
        {
            _candlesManager = candlesManager;
            _assetPairsManager = assetPairsManager;
            _logger = logger;
        }

        public async Task Handle(LimitOrdersMessage message)
        {
            if (message.Orders == null || !message.Orders.Any())
            {
                return;
            }

            var limitOrderIds = message.Orders
                .Select(o => o.Order.Id)
                .ToHashSet();

            foreach (var orderMessage in message.Orders)
            {
                if (orderMessage.Trades == null)
                {
                    continue;
                }

                var assetPair = await _assetPairsManager.TryGetEnabledPairAsync(orderMessage.Order.AssetPairId);

                foreach (var tradeMessage in orderMessage.Trades.OrderBy(t => t.Timestamp).ThenBy(t => t.Index))
                {
                    // If both orders of the trade are limit, then both of them should be contained in the single message,
                    // this is by design.

                    var isOppositeOrderIsLimit = limitOrderIds.Contains(tradeMessage.OppositeOrderId);

                    // If opposite order is market order, then unconditionally takes the given limit order.
                    // But if both of orders are limit orders, we should take only one of them.

                    if (isOppositeOrderIsLimit)
                    {
                        var isBuyOrder = orderMessage.Order.Volume > 0;

                        // Takes trade only for the sell limit orders

                        if (isBuyOrder)
                        {
                            continue;
                        }
                    }

                    // Volumes in the asset pair base and quoting assets
                    double baseVolume;
                    double quotingVolume;

                    if (tradeMessage.Asset == assetPair.BaseAssetId)
                    {
                        baseVolume = tradeMessage.Volume;
                        quotingVolume = tradeMessage.OppositeVolume;
                    }
                    else
                    {
                        baseVolume = tradeMessage.OppositeVolume;
                        quotingVolume = tradeMessage.Volume;
                    }

                    // Just discarding trades with negative prices and\or volumes.  It's better to do it here instead of
                    // at the first line of foreach 'case we have some additional trade selection logic in the begining.
                    // ReSharper disable once InvertIf
                    if (tradeMessage.Price > 0 && baseVolume > 0 && quotingVolume > 0)
                    {
                        var trade = new Trade(
                            orderMessage.Order.AssetPairId,
                            tradeMessage.Timestamp,
                            baseVolume,
                            quotingVolume,
                            tradeMessage.Price
                        );

                        await _candlesManager.ProcessTradeAsync(trade);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Got a spot trade with non-positive price or volume value: {TradeJson}",
                            tradeMessage.ToJson());
                    }
                }
            }
        }
    }
}
