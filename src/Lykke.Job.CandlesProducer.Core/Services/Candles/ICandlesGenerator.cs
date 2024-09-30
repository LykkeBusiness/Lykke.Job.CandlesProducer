// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;

namespace Lykke.Job.CandlesProducer.Core.Services.Candles
{
    public interface ICandlesGenerator : IHaveState<ImmutableDictionary<string, ICandle>>
    {
        CandleUpdateResult UpdateRFactor(ICandle candle, DateTime timestamp, double rFactor);
        CandleUpdateResult UpdateMonthlyOrWeeklyRFactor(ICandle candle, DateTime timestamp, double rFactor);
        CandleUpdateResult UpdateQuotingCandle(string assetPair, DateTime timestamp, double price, CandlePriceType priceType, CandleTimeInterval timeInterval);
        CandleUpdateResult UpdateTradingCandle(string assetPair, DateTime timestamp, double tradePrice, double baseTradingVolume, double quotingTradingVolume, CandleTimeInterval timeInterval);
        void Undo(CandleUpdateResult candleUpdateResult);
        CandleUpdateResult UpsertCandle(string assetPair, 
            DateTime timestamp, 
            double open, 
            double close,
            double low,
            double high, 
            CandlePriceType priceType, 
            CandleTimeInterval timeInterval);
    }
}
