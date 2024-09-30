// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace Lykke.Job.CandlesProducer.Contract.Candles;

public class UpsertCandleRequest
{
    public string ProductId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Price { get; set; }
    public CandlePriceType PriceType { get; set; }
    public CandleTimeInterval TimeInterval { get; set; }
}