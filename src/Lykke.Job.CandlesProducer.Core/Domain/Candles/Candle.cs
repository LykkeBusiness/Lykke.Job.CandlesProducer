﻿using System;
using Lykke.Domain.Prices;
using Newtonsoft.Json;

namespace Lykke.Job.CandlesProducer.Core.Domain.Candles
{
    public class Candle
    {
        [JsonProperty("id")]
        public string AssetPairId { get; set; }
        [JsonProperty("p")]
        public PriceType PriceType { get; set; }
        [JsonProperty("i")]
        public TimeInterval TimeInterval { get; set; }
        [JsonProperty("t")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("o")]
        public double Open { get; set; }
        [JsonProperty("c")]
        public double Close { get; set; }
        [JsonProperty("h")]
        public double High { get; set; }
        [JsonProperty("l")]
        public double Low { get; set; }
    }
}