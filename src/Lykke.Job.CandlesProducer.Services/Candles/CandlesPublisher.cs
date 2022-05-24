// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Serializers;

namespace Lykke.Job.CandlesProducer.Services.Candles
{
    [UsedImplicitly]
    public class CandlesPublisher : ICandlesPublisher
    {
        private RabbitMqPublisher<CandlesUpdatedEvent> _publisher;
        
        private readonly IRabbitMqPublishersFactory _publishersFactory;
        private readonly string _connectionString;
        private readonly string _namespace;
        private readonly string _shardName;
        private readonly string _shardPattern;

        public string ShardName => _shardName;

        public CandlesPublisher(IRabbitMqPublishersFactory publishersFactory, string connectionString, string nspace, string shardName, string shardPattern)
        {
            _publishersFactory = publishersFactory ?? throw new ArgumentNullException(nameof(publishersFactory));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _namespace = nspace ?? throw new ArgumentNullException(nameof(nspace));
            _shardName = shardName ?? throw new ArgumentNullException(nameof(shardName));
            _shardPattern = shardPattern;
        }

        public void Start()
        {
            _publisher = _publishersFactory.Create(
                new MessagePackMessageSerializer<CandlesUpdatedEvent>(),
                _connectionString,
                _namespace,
                $"candles-v2.{_shardName}");
        }

        public Task PublishAsync(IReadOnlyCollection<CandleUpdateResult> updates)
        {
            return PublishV2Async(updates);
        }

        public virtual bool CanPublish(string assetPairId)
        {
            return Regex.IsMatch(
                assetPairId, 
                _shardPattern, 
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }

        private Task PublishV2Async(IEnumerable<CandleUpdateResult> updates)
        {
            var @event = new CandlesUpdatedEvent
            {
                ContractVersion = Contract.Constants.ContractVersion,
                UpdateTimestamp = DateTime.UtcNow,
                Candles = updates
                    .Select(c => new CandleUpdate
                    {
                        IsLatestChange = c.IsLatestChange,
                        ChangeTimestamp = c.Candle.LatestChangeTimestamp,
                        AssetPairId = c.Candle.AssetPairId,
                        PriceType = c.Candle.PriceType,
                        TimeInterval = c.Candle.TimeInterval,
                        CandleTimestamp = c.Candle.Timestamp,
                        Open = c.Candle.Open,
                        Close = c.Candle.Close,
                        Low = c.Candle.Low,
                        High = c.Candle.High,
                        TradingVolume = c.Candle.TradingVolume,
                        TradingOppositeVolume = c.Candle.TradingOppositeVolume,
                        IsLatestCandle = true,
                        LastTradePrice = 0
                    })
                    .ToArray()
            };

            lock (_publisher)
            {
                // HACK: Actually ProduceAsync is not async, so lock works well

                return _publisher.ProduceAsync(@event);
            }
        }

        public void Dispose()
        {
            _publisher?.Dispose();
        }

        public void Stop()
        {
            _publisher?.Stop();
        }
    }
}
