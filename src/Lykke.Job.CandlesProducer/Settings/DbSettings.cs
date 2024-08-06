// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.Job.CandlesProducer.Core.Domain;

namespace Lykke.Job.CandlesProducer.Settings
{
    public class DbSettings
    {
        public StorageMode StorageMode { get; set; }
        public string LogsConnString { get; set; }
        public string SnapshotsConnectionString { get; set; }
    }
}
