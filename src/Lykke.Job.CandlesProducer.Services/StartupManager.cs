// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Cqrs;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Core.Services.Quotes;
using Lykke.Job.CandlesProducer.Core.Services.Trades;

namespace Lykke.Job.CandlesProducer.Services
{
    // TODO: Start MT trades subscriber

    public class StartupManager : IStartupManager
    {
        private readonly IQuotesSubscriber _quotesSubscriber;
        private readonly ITradesSubscriber _tradesSubscriber;
        private readonly IEnumerable<ICandlesPublisher> _candlesPublishers;
        private readonly IEnumerable<ISnapshotSerializer> _snapshotSerializers;
        private readonly IDefaultCandlesPublisher _defaultCandlesPublisher;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly ILog _log;

        public StartupManager(
            IQuotesSubscriber quotesSubscriber,
            ITradesSubscriber tradesSubscriber,
            IEnumerable<ISnapshotSerializer> snapshotSerializers,
            IEnumerable<ICandlesPublisher> candlesPublishers,
            IDefaultCandlesPublisher defaultCandlesPublisher,
            ICqrsEngine cqrsEngine,
            ILog log)
        {
            _quotesSubscriber = quotesSubscriber;
            _tradesSubscriber = tradesSubscriber;
            _candlesPublishers = candlesPublishers;
            _snapshotSerializers = snapshotSerializers;
            _defaultCandlesPublisher = defaultCandlesPublisher;
            _cqrsEngine = cqrsEngine;
            _log = log;
        }

        public  async Task StartAsync()
        {
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Deserializing snapshots async...");

            var snapshotTasks = _snapshotSerializers.Select(s => s.DeserializeAsync()).ToArray();

            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Starting candles publishers...");
            
            _cqrsEngine.StartAll();

            _defaultCandlesPublisher.Start();

            foreach (var candlesPublisher in _candlesPublishers)
            {
                candlesPublisher.Start();
            }
            
            _cqrsEngine.StartAll();
            
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Waiting for snapshots async...");

            await Task.WhenAll(snapshotTasks);

            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Starting quotes subscriber...");

            _quotesSubscriber.Start();

            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Starting trades subscriber...");

            _tradesSubscriber.Start();

            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Started up");
        }
    }
}
