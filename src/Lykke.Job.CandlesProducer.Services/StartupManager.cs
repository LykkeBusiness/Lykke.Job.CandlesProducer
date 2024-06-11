// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;

namespace Lykke.Job.CandlesProducer.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly IEnumerable<ICandlesPublisher> _candlesPublishers;
        private readonly IEnumerable<ISnapshotSerializer> _snapshotSerializers;
        private readonly IDefaultCandlesPublisher _defaultCandlesPublisher;
        private readonly ILog _log;

        public StartupManager(
            IEnumerable<ISnapshotSerializer> snapshotSerializers,
            IEnumerable<ICandlesPublisher> candlesPublishers,
            IDefaultCandlesPublisher defaultCandlesPublisher,
            ILog log)
        {
            _candlesPublishers = candlesPublishers;
            _snapshotSerializers = snapshotSerializers;
            _defaultCandlesPublisher = defaultCandlesPublisher;
            _log = log;
        }

        public  async Task StartAsync()
        {
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Deserializing snapshots async...");

            var snapshotTasks = _snapshotSerializers.Select(s => s.DeserializeAsync()).ToArray();

            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Starting candles publishers...");
            
            _defaultCandlesPublisher.Start();

            foreach (var candlesPublisher in _candlesPublishers)
            {
                candlesPublisher.Start();
            }
            
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Waiting for snapshots async...");

            await Task.WhenAll(snapshotTasks);

            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Started up");
        }
    }
}
