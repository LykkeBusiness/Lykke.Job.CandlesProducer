// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.CandlesProducer.Core.Services;

namespace Lykke.Job.CandlesProducer.Services
{
    public class ShutdownManager : IShutdownManager
    {
        private readonly IEnumerable<ISnapshotSerializer> _snapshotSerializers;
        private readonly ILog _log;

        public ShutdownManager(
            IEnumerable<ISnapshotSerializer> snapshotSerializerses,
            ILog log)
        {
            _snapshotSerializers = snapshotSerializerses;
            _log = log;
        }

        public async Task ShutdownAsync()
        {
            await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Serializing snapshots async...");
            
            var snapshotSrializationTasks = _snapshotSerializers.Select(s  => s.SerializeAsync());

            await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Awaiting for snapshots serialization...");

            await Task.WhenAll(snapshotSrializationTasks);

            await _log.WriteInfoAsync(nameof(ShutdownManager), nameof(ShutdownAsync), "", "Shutted down");
        }
    }
}
