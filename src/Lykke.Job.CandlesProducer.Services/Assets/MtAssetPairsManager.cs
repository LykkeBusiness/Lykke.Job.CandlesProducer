// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Job.CandlesProducer.Core.Services.Assets;
using Lykke.Service.Assets.Client.Custom;

using MarginTrading.AssetService.Contracts;

namespace Lykke.Job.CandlesProducer.Services.Assets
{
    public class MtAssetPairsManager : TimerPeriod, IAssetPairsManager
    {
        private readonly IAssetPairsApi _assetPairsApi;
        
        private Dictionary<string, AssetPair> _cache = new Dictionary<string, AssetPair>();
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        public MtAssetPairsManager(IAssetPairsApi assetPairsApi, TimeSpan expirationPeriod, ILog log)
            : base(nameof(MtAssetPairsManager), (int)expirationPeriod.TotalMilliseconds, log)
        {
            _assetPairsApi = assetPairsApi;
        }

        public Task<IAssetPair> TryGetEnabledPairAsync(string assetPairId)
        {
            if (string.IsNullOrWhiteSpace(assetPairId))
            {
                return Task.FromResult((IAssetPair)null);
            }
            
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return _cache.TryGetValue(assetPairId, out var assetPair)
                    ? Task.FromResult((IAssetPair)assetPair)
                    : Task.FromResult((IAssetPair)null);
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public override void Start()
        {
            base.Start();
            
            Execute().GetAwaiter().GetResult();
        }

        public override async Task Execute()
        {
            var assetPairs = (await _assetPairsApi.List())?
                             .ToDictionary(pair => pair.Id, pair => new AssetPair
                             {
                                 Id = pair.Id,
                                 Name = pair.Name,
                                 BaseAssetId = pair.BaseAssetId,
                                 QuotingAssetId = pair.QuoteAssetId,
                                 Accuracy = pair.Accuracy,
                                 InvertedAccuracy = pair.Accuracy
                             }) ?? new Dictionary<string, AssetPair>();

            _readerWriterLockSlim.EnterWriteLock();
            try
            {
                _cache = assetPairs;
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }
    }
}
