// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.AzureRepositories;
using Lykke.Job.CandlesProducer.Core.Domain;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;

namespace Lykke.Job.CandlesProducer.SqlRepositories
{
    public class SqlCandlesGeneratorSnapshotRepository : ISnapshotRepository<ImmutableDictionary<string, ICandle>>
    {
        private const string BlobContainer = "CandlesGeneratorSnapshot";
        private const string Key = "CandlesGenerator";
        private readonly ICandlesProducerBlobRepository _blobRepository;


        public SqlCandlesGeneratorSnapshotRepository(string connectionString)
        {
            _blobRepository = new SqlBlobRepository(connectionString);
        }

        public Task<ImmutableDictionary<string, ICandle>> TryGetAsync()
        {
            var model = _blobRepository.Read<Dictionary<string, CandleEntity>>(BlobContainer, Key);
            if (model != null)
            {
                return Task.FromResult(model.ToImmutableDictionary(i => i.Key, i => (ICandle)i.Value));
            }

            return Task.FromResult(new Dictionary<string, ICandle>().ToImmutableDictionary());
        }

        public async Task SaveAsync(ImmutableDictionary<string, ICandle> state)
        {
            await _blobRepository.Write(BlobContainer, Key, state);
        }
    }
}
