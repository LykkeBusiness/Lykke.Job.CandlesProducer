// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.CandlesProducer.Settings
{
    public class AssetsSettings
    {
        public string ServiceUrl { get; set; }
        
        [Optional]
        public string ApiKey { get; set; }
    }
}
