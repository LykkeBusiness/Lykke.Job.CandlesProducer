// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Services.Quotes.Mt.Messages;
using Lykke.Job.CandlesProducer.Services.Trades.Mt.Messages;
using Lykke.Job.CandlesProducer.Services.Trades.Spot.Messages;
using Lykke.Job.QuotesProducer.Contract;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.CandlesProducer.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly IEnumerable<ISnapshotSerializer> _snapshotSerializers;
        
        private readonly RabbitMqListener<QuoteMessage> _quoteMessageListener;
        private readonly RabbitMqListener<MtQuoteMessage> _mtQuoteMessageListener;
        private readonly RabbitMqListener<LimitOrdersMessage> _limitOrdersMessageListener;
        private readonly RabbitMqListener<MtTradeMessage> _mtTradeMessageListener;
        
        private readonly IEnumerable<ICandlesPublisher> _candlesPublishers;
        private readonly IDefaultCandlesPublisher _defaultCandlesPublisher;
        
        private readonly ILog _log;

        public StartupManager(
            IEnumerable<ISnapshotSerializer> snapshotSerializers,
            ILog log,
            IEnumerable<ICandlesPublisher> candlesPublishers,
            IDefaultCandlesPublisher defaultCandlesPublisher,
            RabbitMqListener<QuoteMessage> quoteMessageListener = null,
            RabbitMqListener<MtQuoteMessage> mtQuoteMessageListener = null,
            RabbitMqListener<LimitOrdersMessage> limitOrdersMessageListener = null,
            RabbitMqListener<MtTradeMessage> mtTradeMessageListener = null)
        {
            _snapshotSerializers = snapshotSerializers;
            _log = log;
            _quoteMessageListener = quoteMessageListener;
            _mtQuoteMessageListener = mtQuoteMessageListener;
            _limitOrdersMessageListener = limitOrdersMessageListener;
            _mtTradeMessageListener = mtTradeMessageListener;
            _candlesPublishers = candlesPublishers;
            _defaultCandlesPublisher = defaultCandlesPublisher;
        }

        public  async Task StartAsync()
        {
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Deserializing snapshots async...");

            var snapshotTasks = _snapshotSerializers.Select(s => s.DeserializeAsync()).ToArray();
            
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Waiting for snapshots async...");

            await Task.WhenAll(snapshotTasks);
            
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Starting candles publishers...");
            _defaultCandlesPublisher.Start();
            foreach (var candlesPublisher in _candlesPublishers)
            {
                candlesPublisher.Start();
            }
            
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Starting candles listeners...");
            _quoteMessageListener?.Start();
            _mtQuoteMessageListener?.Start();
            _limitOrdersMessageListener?.Start();
            _mtTradeMessageListener?.Start();

            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Started up");
        }
    }
}
