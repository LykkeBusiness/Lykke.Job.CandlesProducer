// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using Lykke.Job.CandlesProducer.Core.Services.Candles;

namespace Lykke.Job.CandlesProducer.Services.Candles
{
    [UsedImplicitly]
    public class CandlesGenerator : ICandlesGenerator
    {
        private readonly ILog _log;
        private readonly TimeSpan _warningsTimeout;
        private ConcurrentDictionary<string, Candle> _candles;
        private readonly ConcurrentDictionary<string, DateTime> _lastWarningTimesForPairs;
        
        public CandlesGenerator(ILog log, TimeSpan warningsTimeout)
        {
            _log = log;
            _warningsTimeout = warningsTimeout;
            _candles = new ConcurrentDictionary<string, Candle>();
            _lastWarningTimesForPairs = new ConcurrentDictionary<string, DateTime>();
        }

        public CandleUpdateResult UpdateRFactor(ICandle candle, DateTime timestamp, double rFactor)
        {
            return Update(candle.AssetPairId, timestamp, candle.PriceType, candle.TimeInterval,
                createNewCandle: () => Candle.Copy(candle).UpdateRFactor(timestamp, rFactor),
                updateCandle: oldCandle => oldCandle.UpdateRFactor(timestamp, rFactor),
                getLoggingContext: c => new
                {
                    candle = c,
                    timestamp,
                    rFactor
                });
        }

        public CandleUpdateResult UpdateMonthlyOrWeeklyRFactor(ICandle candle, DateTime timestamp, double rFactor)
        {
            return Update(candle.AssetPairId, timestamp, candle.PriceType, candle.TimeInterval,
                createNewCandle: () => Candle.Copy(candle).UpdateMonthlyOrWeeklyRFactor(timestamp, rFactor),
                updateCandle: oldCandle => oldCandle.UpdateMonthlyOrWeeklyRFactor(timestamp, rFactor),
                getLoggingContext: candle => new
                {
                    candle = candle,
                    timestamp = timestamp,
                    rFactor = rFactor
                });
        }

        public CandleUpdateResult UpdateQuotingCandle(string assetPair, DateTime timestamp, double price, CandlePriceType priceType, CandleTimeInterval timeInterval)
        {
            // We can update LastTradePrice for only Trades candle below:
            return Update(assetPair, timestamp, priceType, timeInterval,
                createNewCandle: () => Candle.CreateQuotingCandle(assetPair, timestamp, price, priceType, timeInterval),
                updateCandle: oldCandle => oldCandle.UpdateQuotingCandle(timestamp, price),
                getLoggingContext: candle => new
                {
                    assetPair,
                    timestamp,
                    price,
                    priceType,
                    timeInterval
                });
        }

        public CandleUpdateResult UpdateTradingCandle(string assetPair, DateTime timestamp, double tradePrice, double baseTradingVolume, double quotingTradingVolume, CandleTimeInterval timeInterval)
        {
            return Update(assetPair, timestamp, CandlePriceType.Trades, timeInterval,
                createNewCandle: () => Candle.CreateTradingCandle(assetPair, timestamp, tradePrice, baseTradingVolume, quotingTradingVolume, timeInterval), 
                updateCandle: oldCandle => oldCandle.UpdateTradingCandle(timestamp, tradePrice, baseTradingVolume, quotingTradingVolume),
                getLoggingContext: candle => new
                {
                    assetPair,
                    timestamp,
                    tradePrice,
                    baseTradingVolume,
                    quotingTradingVolume,
                    timeInterval
                });
        }
        
        public void Undo(CandleUpdateResult candleUpdateResult)
        {
            if (!candleUpdateResult.WasChanged)
            {
                return;
            }

            var candle = candleUpdateResult.Candle;
            var key = GetKey(candle.AssetPairId, candle.TimeInterval, candle.PriceType);

            // Key should be presented in the dictionary, since Update was called before and keys are never removed

            _candles.AddOrUpdate(
                key,
                addValueFactory: k => throw new InvalidOperationException("Key should be already presented in the dictionary"),
                updateValueFactory: (k, candles) => candleUpdateResult.OldCandle);
        }

        public ImmutableDictionary<string, ICandle> GetState()
        {
            return _candles.ToImmutableDictionary(i => i.Key, i => (ICandle)i.Value);
        }

        public void SetState(ImmutableDictionary<string, ICandle> state)
        {
            if (state?.IsEmpty ?? true)
                return;
            
            if (!_candles.IsEmpty)
            {
                throw new InvalidOperationException("Candles generator state already not empty");
            }

            var keyValuePairs = state.Select(i => KeyValuePair.Create(i.Key, Candle.Copy(i.Value)));

            _candles = new ConcurrentDictionary<string, Candle>(keyValuePairs);
        }

        public string DescribeState(ImmutableDictionary<string, ICandle> state)
        {
            return $"Candles count: {state.Count}";
        }

        private CandleUpdateResult Update(
            string assetPair,
            DateTime timestamp,
            CandlePriceType priceType,
            CandleTimeInterval timeInterval,
            Func<Candle> createNewCandle,
            Func<Candle, Candle> updateCandle,
            Func<Candle, object> getLoggingContext)
        {
            var key = GetKey(assetPair, timeInterval, priceType);

            // Let's prepare the old and the updated versions of our candle to compare in the very end.
            _candles.TryGetValue(key, out var oldCandle);

            var newCandle = _candles.AddOrUpdate(key,
                addValueFactory: k => createNewCandle(),
                updateValueFactory: (k, candle) =>
                {
                    // Candles is identified by the Timestamp

                    var candleTimestamp = timestamp.TruncateTo(timeInterval);

                    if (candleTimestamp == candle.Timestamp)
                    {
                        // Candle matches exactly - updating it

                        return updateCandle(candle);
                    }

                    if (candleTimestamp > candle.Timestamp)
                    {
                        // The timestamp is newer than the candle we have - creating new candle instead

                        return createNewCandle();
                    }
                    
                    // Given data is older then the cached candle.
                    // Nothing to update here and no candle can be returned
                    // since we can't obtain full candle state. Let's see also,
                    // if we need to log down this event.

                    if (_lastWarningTimesForPairs.TryGetValue(candle.AssetPairId, out var lastWarningTime) &&
                        DateTime.UtcNow - lastWarningTime <= _warningsTimeout)
                        return candle;

                    _lastWarningTimesForPairs[candle.AssetPairId] = DateTime.UtcNow;
                    _log.WriteWarningAsync(
                        nameof(CandlesGenerator),
                        nameof(Update),
                        getLoggingContext(candle).ToJson(),
                        $"Incoming data is too old to update the candle. No candle will be altered. Original timestamp: {timestamp:O}. New candle timestamp: {candleTimestamp:O}. Existing candle timestamp: {candle.Timestamp:O}. Existing candle last update: {candle.LatestChangeTimestamp:O}").Wait();

                    return candle;
                });

            // Candles without prices shouldn't be produced
            return new CandleUpdateResult(
                newCandle,
                oldCandle,
                wasChanged: !newCandle.Equals(oldCandle),
                isLatestChange: oldCandle == null || newCandle.LatestChangeTimestamp >= oldCandle.LatestChangeTimestamp);
        }

        private static string GetKey(string assetPairId, CandleTimeInterval timeInterval, CandlePriceType priceType)
        {
            return $"{assetPairId.Trim()}-{priceType}-{timeInterval}";
        }
    }
}
