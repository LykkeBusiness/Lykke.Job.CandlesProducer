// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Core.Domain;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using MessagePack;

namespace Lykke.Job.CandlesProducer.AzureRepositories
{
    [UsedImplicitly]
    public class CandlesGeneratorSnapshotRepository : ISnapshotRepository<ImmutableDictionary<string, ICandle>>
    {
        private const string Key = "CandlesGenerator";

        private readonly ILog _log;
        private readonly IBlobStorage _storage;

        public CandlesGeneratorSnapshotRepository(ILog log, IBlobStorage storage)
        {
            _log = log;
            _storage = storage;
        }

        public async Task SaveAsync(ImmutableDictionary<string, ICandle> state)
        {
            using (var stream = new MemoryStream())
            {
                var model = state.ToDictionary(i => i.Key, i => CandleEntity.Copy(i.Value));

                MessagePackSerializer.Serialize(stream, model);

                await stream.FlushAsync();
                stream.Seek(0, SeekOrigin.Begin);

                await _storage.SaveBlobAsync(Constants.SnapshotsContainer, Key, stream);
            }
        }

        public async Task<ImmutableDictionary<string, ICandle>> TryGetAsync()
        {
            if (!await _storage.HasBlobAsync(Constants.SnapshotsContainer, Key))
            {
                return null;
            }

            try
            {
                using (var stream = await _storage.GetAsync(Constants.SnapshotsContainer, Key))
                {
                    var model = MessagePackSerializer.Deserialize<Dictionary<string, CandleEntity>>(stream);

                    return model.ToImmutableDictionary(i => i.Key, i => (ICandle)i.Value);
                }
            }
            catch (InvalidOperationException)
            {
                await _log.WriteWarningAsync(
                    nameof(CandlesGeneratorSnapshotRepository),
                    nameof(TryGetAsync), 
                    "Failed to deserialize the candles generator snapshot, trying to deserialize it as the legacy format");

                return await DeserializeLegacyFormat();
            }
        }

        private async Task<ImmutableDictionary<string, ICandle>> DeserializeLegacyFormat()
        {
            using (var stream = await _storage.GetAsync(Constants.SnapshotsContainer, Key))
            {
                var model = MessagePackSerializer.Deserialize<Dictionary<string, IEnumerable<CandleEntity>>>(stream);

                return model
                    .Select(x => new
                    {
                        x.Key,
                        Candle = (ICandle) x.Value.OrderBy(c => c.Timestamp).LastOrDefault()
                    })
                    .Where(x => x.Candle != null)
                    .ToImmutableDictionary(x => x.Key, x => x.Candle);
            }
        }
    }
}
