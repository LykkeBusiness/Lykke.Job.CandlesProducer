// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Core.Domain.Trades;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Assets;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Core.Services.Trades;
using Lykke.Job.CandlesProducer.Services.Helpers;
using Lykke.Job.CandlesProducer.Services.Trades.Spot.Messages;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.CandlesProducer.Services.Trades.Spot
{
    [UsedImplicitly]
    public class SpotTradesSubscriber : ITradesSubscriber
    {
        private readonly ILog _log;
        private readonly ICandlesManager _candlesManager;
        private readonly IRabbitMqSubscribersFactory _subscribersFactory;
        private readonly IRabbitSubscriptionSettings _tradesSubscriptionSettings;
        private readonly IAssetPairsManager _assetPairsManager;
        private IStartStop _limitTradesSubscriber;

        public SpotTradesSubscriber(
            ILog log,
            ICandlesManager candlesManager, 
            IRabbitMqSubscribersFactory subscribersFactory, 
            IRabbitSubscriptionSettings tradesSubscriptionSettings,
            IAssetPairsManager assetPairsManager)
        {
            _log = log?.CreateComponentScope(nameof(SpotTradesSubscriber)) ??
                   throw new ArgumentNullException(nameof(log));
            _candlesManager = candlesManager ?? throw new ArgumentNullException(nameof(candlesManager));
            _subscribersFactory = subscribersFactory ?? throw new ArgumentNullException(nameof(_subscribersFactory));
            _tradesSubscriptionSettings = tradesSubscriptionSettings ?? throw new ArgumentNullException(nameof(_tradesSubscriptionSettings));
            _assetPairsManager = assetPairsManager ?? throw new ArgumentNullException(nameof(_assetPairsManager));
        }

        private RabbitMqSubscriptionSettings _subscriptionSettings;
        public RabbitMqSubscriptionSettings SubscriptionSettings
        {
            get
            {
                if (_subscriptionSettings == null)
                {
                    _subscriptionSettings = RabbitMqSubscriptionSettingsHelper.GetSubscriptionSettings(_tradesSubscriptionSettings.ConnectionString, "lykke", "limitorders.clients", "-v2");
                }
                return _subscriptionSettings;
            }
        }

        public void Start()
        {
            _limitTradesSubscriber = _subscribersFactory.Create<LimitOrdersMessage>(SubscriptionSettings, ProcessLimitTradesAsync);
        }

        private async Task ProcessLimitTradesAsync(LimitOrdersMessage message)
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
                        await _log.WriteWarningAsync(nameof(ProcessLimitTradesAsync), tradeMessage.ToJson(), "Got a Spot trade with non-positive price or volume value.");
                    }
                }
            }
        }
        
        public void Stop()
        {
            _limitTradesSubscriber?.Stop();
        }

        public void Dispose()
        {
            _limitTradesSubscriber?.Dispose();
        }
    }
}
