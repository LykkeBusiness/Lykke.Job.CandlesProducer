// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Core.Domain;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using Lykke.Job.CandlesProducer.AzureRepositories;

namespace Lykke.Job.CandlesProducer.SqlRepositories
{
    public class SqlMidPriceQuoteGeneratorSnapshotRepository : ISnapshotRepository<IImmutableDictionary<string, IMarketState>>
    {
        private const string BlobContainer = "MidPriceQuoteGeneratorSnapshot";
        private const string Key = "MidPriceQuoteGenerator";
        private readonly ICandlesProducerBlobRepository _blobRepository;


        public SqlMidPriceQuoteGeneratorSnapshotRepository( string connectionString)
        {
            _blobRepository = new SqlBlobRepository(connectionString);
        }
    
        public async Task<IImmutableDictionary<string, IMarketState>> TryGetAsync()
        {
            var model = _blobRepository.Read<Dictionary<string, MarketStateEntity>>(BlobContainer, Key);
            if (model != null)
            {
                model.ToImmutableDictionary(i => i.Key, i => (IMarketState)i.Value);
                

            }
            return new Dictionary<string, IMarketState>().ToImmutableDictionary();
        }

        public async Task SaveAsync(IImmutableDictionary<string, IMarketState> state)
        {
           await _blobRepository.Write(BlobContainer, Key, state);
        }
    }

}
