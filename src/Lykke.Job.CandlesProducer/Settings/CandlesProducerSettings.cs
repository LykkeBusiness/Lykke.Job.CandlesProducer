// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Core.Settings;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.CandlesProducer.Settings
{
    [UsedImplicitly]
    public class CandlesProducerSettings
    {
        [Optional, CanBeNull]
        public ResourceMonitorSettings ResourceMonitor { get; set; }
        
        public DbSettings Db { get; set; }
        
        public AssetsCacheSettings AssetsCache { get; set; }
        
        public RabbitSettings Rabbit { get; set; }
        
        public CandlesGeneratorSettings CandlesGenerator { get; set; }
        
        [Optional]
        public bool UseSerilog { get; set; }

        [Optional]
        public bool SkipEodQuote { get; set; } = true;
        
        public CqrsSettings Cqrs { get; set; }
    }
}
