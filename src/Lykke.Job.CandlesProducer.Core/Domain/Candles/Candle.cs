// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.Job.CandlesProducer.Contract;

namespace Lykke.Job.CandlesProducer.Core.Domain.Candles
{
    public class Candle : IEquatable<Candle>, ICandle
    {
        public string AssetPairId { get; }
        public CandlePriceType PriceType { get; }
        public CandleTimeInterval TimeInterval { get; }
        public DateTime Timestamp { get; }
        public double Open { get; }
        public double Close { get; }
        public double High { get; }
        public double Low { get; }
        public double TradingVolume { get; }
        public double TradingOppositeVolume { get; }
        public DateTime LatestChangeTimestamp { get; }
        public DateTime OpenTimestamp { get; }

        private Candle(
            string assetPairId,
            CandlePriceType priceType,
            CandleTimeInterval timeInterval,
            DateTime timestamp,
            DateTime latestChangeTimestamp,
            DateTime openTimestamp,
            double open,
            double close,
            double low,
            double high,
            double tradingVolume,
            double tradingOppositeVolume)
        {
            AssetPairId = assetPairId;
            PriceType = priceType;
            TimeInterval = timeInterval;
            Timestamp = timestamp;
            LatestChangeTimestamp = latestChangeTimestamp;
            OpenTimestamp = openTimestamp;
            Open = open;
            Close = close;
            Low = low;
            High = high;
            TradingVolume = tradingVolume;
            TradingOppositeVolume = tradingOppositeVolume;
        }

        public static Candle Copy(ICandle candle)
        {
            return new Candle
            (
                candle.AssetPairId,
                candle.PriceType,
                candle.TimeInterval,
                candle.Timestamp,
                candle.LatestChangeTimestamp,
                candle.OpenTimestamp,
                candle.Open,
                candle.Close,
                candle.Low,
                candle.High,
                candle.TradingVolume,
                candle.TradingOppositeVolume
            );
        }

        public static Candle CreateQuotingCandle(
            string assetPair,
            DateTime timestamp,
            double price,
            CandlePriceType priceType,
            CandleTimeInterval timeInterval)
        {
            if (priceType != CandlePriceType.Ask &&
                priceType != CandlePriceType.Bid &&
                priceType != CandlePriceType.Mid)
            {
                throw new ArgumentOutOfRangeException(nameof(priceType), priceType,
                    "Price type should be Ask, Bid or Mid for the quoting candle");
            }

            var intervalTimestamp = timestamp.TruncateTo(timeInterval);

            return new Candle
            (
                assetPair,
                priceType,
                timeInterval,
                intervalTimestamp,
                timestamp,
                timestamp,
                price,
                price,
                price,
                price,
                0,
                0
            );
        }

        public static Candle CreateTradingCandle(
            string assetPair,
            DateTime timestamp,
            double price,
            double baseTradingVolume,
            double quotingTradingVolume,
            CandleTimeInterval timeInterval)
        {
            var intervalTimestamp = timestamp.TruncateTo(timeInterval);

            return new Candle
            (
                assetPair,
                CandlePriceType.Trades,
                timeInterval,
                intervalTimestamp,
                timestamp,
                timestamp,
                price,
                price,
                price,
                price,
                baseTradingVolume,
                quotingTradingVolume
            );
        }

        public Candle UpdateRFactor(DateTime timestamp, double rFactor)
        {
            if (rFactor <= 0)
            {
                throw new ArgumentException("R-factor should be greater than 0.");
            }

            if (timestamp < LatestChangeTimestamp)
            {
                throw new ArgumentException("Timestamp shouldn't be less than LatestChangeTimestamp.");
            }

            return new Candle(
                AssetPairId,
                PriceType,
                TimeInterval,
                Timestamp,
                timestamp,
                OpenTimestamp,
                Open * rFactor,
                Close * rFactor,
                Low * rFactor,
                High * rFactor,
                TradingVolume,
                TradingOppositeVolume);
        }

        public Candle UpdateMonthlyOrWeeklyRFactor(DateTime timestamp, double rFactor)
        {
            if (rFactor <= 0)
            {
                throw new ArgumentException("R-factor should be greater than 0.");
            }

            if (timestamp < LatestChangeTimestamp)
            {
                throw new ArgumentException("Timestamp shouldn't be less than LatestChangeTimestamp.");
            }

            if (TimeInterval != CandleTimeInterval.Month && TimeInterval != CandleTimeInterval.Week) return this;

            var low = Low;
            var high = High;
            var open = Open;

            open *= rFactor;
            if (rFactor > 1)
            {
                low *= rFactor;
            }
            else
            {
                high *= rFactor;
            }

            return new Candle(
                AssetPairId,
                PriceType,
                TimeInterval,
                Timestamp,
                timestamp,
                OpenTimestamp,
                open,
                Close,
                low,
                high,
                TradingVolume,
                TradingOppositeVolume);
        }

        public Candle UpdateQuotingCandle(DateTime timestamp, double price)
        {
            if (PriceType != CandlePriceType.Ask &&
                PriceType != CandlePriceType.Bid &&
                PriceType != CandlePriceType.Mid)
            {
                throw new InvalidOperationException("Price type should be Ask, Bid or Mid for the quoting candle");
            }

            double closePrice;
            double openPrice;
            double lowPrice;
            double highPrice;

            closePrice = LatestChangeTimestamp <= timestamp ? price : Close;
            openPrice = OpenTimestamp > timestamp ? price : Open;
            lowPrice = Math.Min(Low, price);
            highPrice = Math.Max(High, price);

            var changeTimestamp = LatestChangeTimestamp < timestamp ? timestamp : LatestChangeTimestamp;
            var openTimestamp = OpenTimestamp > timestamp ? timestamp : OpenTimestamp;

            return new Candle(
                AssetPairId,
                PriceType,
                TimeInterval,
                Timestamp,
                changeTimestamp,
                openTimestamp,
                openPrice,
                closePrice,
                lowPrice,
                highPrice,
                0,
                0);
        }

        public Candle UpdateTradingCandle(DateTime timestamp, double price, double tradingVolume,
            double tradingOppositeVolume)
        {
            if (PriceType != CandlePriceType.Trades)
            {
                throw new InvalidOperationException("Price type should be Trades for the trading candle");
            }

            double closePrice;
            double openPrice;
            double lowPrice;
            double highPrice;

            closePrice = LatestChangeTimestamp <= timestamp ? price : Close;
            openPrice = OpenTimestamp > timestamp ? price : Open;
            lowPrice = Math.Min(Low, price);
            highPrice = Math.Max(High, price);

            var changeTimestamp = LatestChangeTimestamp < timestamp ? timestamp : LatestChangeTimestamp;
            var openTimestamp = OpenTimestamp > timestamp ? timestamp : OpenTimestamp;

            return new Candle(
                AssetPairId,
                PriceType,
                TimeInterval,
                Timestamp,
                changeTimestamp,
                openTimestamp,
                openPrice,
                closePrice,
                lowPrice,
                highPrice,
                TradingVolume + tradingVolume,
                TradingOppositeVolume + tradingOppositeVolume);
        }

        public bool Equals(Candle other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(AssetPairId, other.AssetPairId) &&
                   PriceType == other.PriceType &&
                   TimeInterval == other.TimeInterval &&
                   Timestamp.Equals(other.Timestamp) &&
                   LatestChangeTimestamp.Equals(other.LatestChangeTimestamp) &&
                   Open.Equals(other.Open) &&
                   Close.Equals(other.Close) &&
                   High.Equals(other.High) &&
                   Low.Equals(other.Low) &&
                   TradingVolume.Equals(other.TradingVolume) &&
                   TradingOppositeVolume.Equals(other.TradingOppositeVolume);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Candle)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AssetPairId != null ? AssetPairId.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (int)PriceType;
                hashCode = (hashCode * 397) ^ (int)TimeInterval;
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ LatestChangeTimestamp.GetHashCode();
                hashCode = (hashCode * 397) ^ Open.GetHashCode();
                hashCode = (hashCode * 397) ^ Close.GetHashCode();
                hashCode = (hashCode * 397) ^ High.GetHashCode();
                hashCode = (hashCode * 397) ^ Low.GetHashCode();
                hashCode = (hashCode * 397) ^ TradingVolume.GetHashCode();
                hashCode = (hashCode * 397) ^ TradingOppositeVolume.GetHashCode();

                return hashCode;
            }
        }
    }
}
