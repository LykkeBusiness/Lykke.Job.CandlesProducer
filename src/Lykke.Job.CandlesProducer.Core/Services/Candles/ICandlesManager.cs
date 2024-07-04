// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Core.Domain.Quotes;
using Lykke.Job.CandlesProducer.Core.Domain.Trades;
using Lykke.Job.QuotesProducer.Contract;

namespace Lykke.Job.CandlesProducer.Core.Services.Candles
{
    public interface ICandlesManager
    {
        Task ProcessMtQuoteAsync(MtQuoteDto mtQuote);
        Task ProcessSpotQuoteAsync(QuoteMessage quote);
        Task ProcessTradeAsync(Trade trade);
        Task UpdateRFactor(string assetPair, double rFactor);
        Task UpdateMonthlyOrWeeklyRFactor(string assetPair, double rFactor, DateTime rFactorDate,
            DateTime lastTradingDate);
    }
}
