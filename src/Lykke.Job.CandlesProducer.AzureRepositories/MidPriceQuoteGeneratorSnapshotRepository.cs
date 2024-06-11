// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Job.CandlesProducer.Core.Domain;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using MessagePack;

namespace Lykke.Job.CandlesProducer.AzureRepositories
{
    public class MidPriceQuoteGeneratorSnapshotRepository : ISnapshotRepository<IImmutableDictionary<string, IMarketState>>
    {
        private const string Key = "MidPriceQuoteGenerator";

        private readonly IBlobStorage _storage;

        public MidPriceQuoteGeneratorSnapshotRepository(IBlobStorage storage)
        {
            _storage = storage;
        }

        public async Task SaveAsync(IImmutableDictionary<string, IMarketState> state)
        {
            using (var stream = new MemoryStream())
            {
                var model = state.ToDictionary(i => i.Key, i => MarketStateEntity.Create(i.Value));

                MessagePackSerializer.Serialize(stream, model);

                await stream.FlushAsync();
                stream.Seek(0, SeekOrigin.Begin);

                await _storage.SaveBlobAsync(Constants.SnapshotsContainer, Key, stream);
            }
        }

        public async Task<IImmutableDictionary<string, IMarketState>> TryGetAsync()
        {
            if (!await _storage.HasBlobAsync(Constants.SnapshotsContainer, Key))
            {
                return null;
            }

            using (var stream = await _storage.GetAsync(Constants.SnapshotsContainer, Key))
            {
                var model = MessagePackSerializer.Deserialize<Dictionary<string, MarketStateEntity>>(stream);

                return model.ToImmutableDictionary(i => i.Key, i => (IMarketState) i.Value);
            }
        }
    }
}
