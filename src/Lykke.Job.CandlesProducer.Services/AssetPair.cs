// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.Service.Assets.Client.Custom;

namespace Lykke.Job.CandlesProducer.Services
{
    public class AssetPair : IAssetPair
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string BaseAssetId { get; set; }

        public string QuotingAssetId { get; set; }

        public int Accuracy { get; set; }

        public int InvertedAccuracy { get; set; }

        public string Source { get; set; }

        public string Source2 { get; set; }

        public bool IsDisabled { get; set; }
    }
}
