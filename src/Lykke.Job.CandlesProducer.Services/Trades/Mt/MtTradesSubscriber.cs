// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Job.CandlesProducer.Core.Domain.Trades;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Core.Services.Trades;
using Lykke.Job.CandlesProducer.Services.Helpers;
using Lykke.Job.CandlesProducer.Services.Trades.Mt.Messages;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.CandlesProducer.Services.Trades.Mt
{
    [UsedImplicitly]
    public class MtTradesSubscriber : ITradesSubscriber
    {
        private readonly ILog _log;
        private readonly ICandlesManager _candlesManager;
        private readonly IRabbitMqSubscribersFactory _subscribersFactory;
        private readonly string _connectionString;
        private readonly bool _isEnabled;
        private IStartStop _tradesSubscriber;

        public MtTradesSubscriber(ILog log,
            ICandlesManager candlesManager,
            IRabbitMqSubscribersFactory subscribersFactory,
            string connectionString,
            bool isEnabled)
        {
            _log = log?.CreateComponentScope(nameof(MtTradesSubscriber)) ?? throw new ArgumentNullException(nameof(log));
            _candlesManager = candlesManager;
            _subscribersFactory = subscribersFactory;
            _connectionString = connectionString;
            _isEnabled = isEnabled;
        }

        private RabbitMqSubscriptionSettings _subscriptionSettings;
        public RabbitMqSubscriptionSettings SubscriptionSettings
        {
            get
            {
                if (_subscriptionSettings == null)
                {
                    _subscriptionSettings = RabbitMqSubscriptionSettingsHelper.GetSubscriptionSettings(_connectionString, "lykke.mt", "trades", "-v2");
                }
                return _subscriptionSettings;
            }
        }

        public void Start()
        {
            if (_isEnabled)
                _tradesSubscriber = _subscribersFactory.Create<MtTradeMessage>(SubscriptionSettings, ProcessTradeAsync);
        }

        private async Task ProcessTradeAsync(MtTradeMessage message)
        {
            // Just discarding trades with negative or zero prices and\or volumes.
            if (message.Price <= 0 ||
                message.Volume <= 0)
            {
                await _log.WriteWarningAsync(nameof(ProcessTradeAsync), message.ToJson(), "Got an MT trade with non-positive price or volume value.");
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

        public void Dispose()
        {
            _tradesSubscriber?.Dispose();
        }

        public void Stop()
        {
            _tradesSubscriber?.Stop();
        }
    }
}
