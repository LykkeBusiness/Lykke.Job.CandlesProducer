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
    public class ShutdownManager : IShutdownManager
    {
        private readonly IEnumerable<ISnapshotSerializer> _snapshotSerializers;
        private readonly IEnumerable<ICandlesPublisher> _candlesPublishers;
        private readonly IDefaultCandlesPublisher _defaultCandlesPublisher;
        private readonly ILog _log;

        public ShutdownManager(
            IEnumerable<ISnapshotSerializer> snapshotSerializerses,
            IEnumerable<ICandlesPublisher> candlesPublishers, 
            IDefaultCandlesPublisher defaultCandlesPublisher,
            ILog log)
        {
            _snapshotSerializers = snapshotSerializerses;
            _log = log;
            _candlesPublishers = candlesPublishers;
            _defaultCandlesPublisher = defaultCandlesPublisher;
        }

        public async Task ShutdownAsync()
        {
            await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Serializing snapshots async...");
            
            var snapshotSrializationTasks = _snapshotSerializers.Select(s  => s.SerializeAsync());

            await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Stopping candles publishers...");

            _defaultCandlesPublisher.Stop();
            
            foreach (var candlesPublisher in _candlesPublishers)
            {
                candlesPublisher.Stop();
            }

            await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Awaiting for snapshots serialization...");

            await Task.WhenAll(snapshotSrializationTasks);

            await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Shutted down");
        }
    }
}
