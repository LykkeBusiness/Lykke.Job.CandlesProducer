// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Core.Services.Quotes;
using Lykke.Job.CandlesProducer.Services.Helpers;
using Lykke.Job.QuotesProducer.Contract;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.CandlesProducer.Services.Quotes.Spot
{
    public class SpotQuotesSubscriber : IQuotesSubscriber
    {
        private readonly ILog _log;
        private readonly ICandlesManager _candlesManager;
        private readonly string _connectionString;

        private IStartStop _subscriber;

        public SpotQuotesSubscriber(ILog log, ICandlesManager candlesManager, string connectionString)
        {
            _log = log;
            _candlesManager = candlesManager;
            _connectionString = connectionString;
        }

        private RabbitMqSubscriptionSettings _subscriptionSettings;
        public RabbitMqSubscriptionSettings SubscriptionSettings
        {
            get
            {
                if (_subscriptionSettings == null)
                {
                    _subscriptionSettings = RabbitMqSubscriptionSettingsHelper.GetSubscriptionSettings(_connectionString, "lykke", "quotefeed");
                }
                return _subscriptionSettings;
            }
        }

        public void Start()
        {
            //_subscriber = _subscribersFactory.Create<QuoteMessage>(SubscriptionSettings, ProcessQuoteAsync);
        }

        public void Stop()
        {
            _subscriber?.Stop();
        }

        private async Task ProcessQuoteAsync(QuoteMessage quote)
        {
            try
            {
                var validationErrors = ValidateQuote(quote);
                if (validationErrors.Any())
                {
                    var message = string.Join("\r\n", validationErrors);
                    await _log.WriteWarningAsync(nameof(SpotQuotesSubscriber), nameof(ProcessQuoteAsync), quote.ToJson(), message);

                    return;
                }

                await _candlesManager.ProcessSpotQuoteAsync(quote);
            }
            catch (Exception)
            {
                await _log.WriteWarningAsync(nameof(SpotQuotesSubscriber), nameof(ProcessQuoteAsync), quote.ToJson(), "Failed to process quote");
                throw;
            }
        }

        private static IReadOnlyCollection<string> ValidateQuote(QuoteMessage quote)
        {
            var errors = new List<string>();

            if (quote == null)
            {
                errors.Add("Quote is null.");
            }
            else
            {
                if (string.IsNullOrEmpty(quote.AssetPair))
                {
                    errors.Add("Empty 'AssetPair'");
                }
                if (quote.Timestamp.Kind != DateTimeKind.Utc)
                {
                    errors.Add($"Invalid 'Timestamp' Kind (UTC is required): '{quote.Timestamp.Kind}'");
                }
                if (quote.Price <= 0)
                {
                    errors.Add($"Not positive price: '{quote.Price}'");
                }
            }

            return errors;
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
        }
    }
}
