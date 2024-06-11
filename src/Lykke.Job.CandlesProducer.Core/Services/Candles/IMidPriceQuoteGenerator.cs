// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using Lykke.Job.QuotesProducer.Contract;

namespace Lykke.Job.CandlesProducer.Core.Services.Candles
{
    public interface IMidPriceQuoteGenerator : IHaveState<IImmutableDictionary<string, IMarketState>>
    {
        QuoteMessage TryGenerate(string assetPair, double ask, double bid, DateTime timestamp, int assetPairAccuracy);
        
        QuoteMessage TryGenerate(string assetPair, bool isBuy, double price, DateTime timestamp, int assetPairAccuracy);
    }
}
