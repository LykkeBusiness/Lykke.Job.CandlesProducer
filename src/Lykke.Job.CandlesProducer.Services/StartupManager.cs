// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.CandlesProducer.Core.Services;

namespace Lykke.Job.CandlesProducer.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly IEnumerable<ISnapshotSerializer> _snapshotSerializers;
        private readonly ILog _log;

        public StartupManager(
            IEnumerable<ISnapshotSerializer> snapshotSerializers,
            ILog log)
        {
            _snapshotSerializers = snapshotSerializers;
            _log = log;
        }

        public  async Task StartAsync()
        {
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Deserializing snapshots async...");

            var snapshotTasks = _snapshotSerializers.Select(s => s.DeserializeAsync()).ToArray();
            
            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Waiting for snapshots async...");

            await Task.WhenAll(snapshotTasks);

            await _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "", "Started up");
        }
    }
}
