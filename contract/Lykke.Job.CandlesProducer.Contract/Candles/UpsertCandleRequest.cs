// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Lykke.Job.CandlesProducer.Contract.Candles;

public class UpsertCandleRequest
{
    [Required] public string ProductId { get; set; }
    [Required] public DateTime Timestamp { get; set; }

    [Required]
    [Range(0.0, Double.PositiveInfinity, ErrorMessage = "The field {0} should be greater than {1}")]
    public double Open { get; set; }

    [Required]
    [Range(0.0, Double.PositiveInfinity, ErrorMessage = "The field {0} should be greater than {1}")]
    public double Close { get; set; }

    [Required]
    [Range(0.0, Double.PositiveInfinity, ErrorMessage = "The field {0} should be greater than {1}")]
    public double Low { get; set; }

    [Required]
    [Range(0.0, Double.PositiveInfinity, ErrorMessage = "The field {0} should be greater than {1}")]
    public double High { get; set; }

    [Required] public CandlePriceType PriceType { get; set; }
    [Required] public CandleTimeInterval TimeInterval { get; set; }
}