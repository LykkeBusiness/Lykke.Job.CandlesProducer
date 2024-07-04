// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MessagePack;

// ReSharper disable once CheckNamespace
namespace CorporateActions.Broker.Contracts.Workflow
{
    [MessagePackObject]
    public class UpdateCurrentCandlesCommand
    {
        [Key(0)] public string TaskId { get; set; }
        [Key(1)] public string ProductId { get; set; }
        [Key(2)] public DateTime RFactorDate { get; set; }

        [Key(3)] public decimal RFactor { get; set; }
        [Key(4)] public DateTime LastTradingDate { get; set; }
    }

    [MessagePackObject]
    public class CurrentCandlesUpdatedEvent
    {
        [Key(0)] public string TaskId { get; set; }
        
        [Key(1)] public string ProductId { get; set; }
        [Key(2)] public DateTime RFactorDate { get; set; }

        [Key(3)] public decimal RFactor { get; set; }
        [Key(4)] public DateTime? LastTradingDate { get; set; }
        [Key(5)] public List<CandleUpdatedResult> UpdatedCandles { get; set; }
    }

    [MessagePackObject]
    public class CandleUpdatedResult
    {
        [Key(0)]
        public int TimeInterval { get; set; }
        [Key(1)]
        public DateTime Timestamp { get; set; }
    }
}
