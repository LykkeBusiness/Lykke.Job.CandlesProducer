// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Job.CandlesProducer.Core.Domain.Quotes;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Core.Services.Quotes;
using Lykke.Job.CandlesProducer.Services.Helpers;
using Lykke.Job.CandlesProducer.Services.Quotes.Mt.Messages;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Job.CandlesProducer.Services.Quotes.Mt
{
    public class MtQuotesSubscriber : IQuotesSubscriber
    {
        private readonly ILog _log;
        private readonly ICandlesManager _candlesManager;
        private readonly IRabbitMqSubscribersFactory _subscribersFactory;
        private readonly string _connectionString;
        private readonly bool _skipEodQuote;

        private IStartStop _subscriber;

        public MtQuotesSubscriber(ILog log, ICandlesManager candlesManager, 
            IRabbitMqSubscribersFactory subscribersFactory, string connectionString, bool skipEodQuote)
        {
            _log = log;
            _candlesManager = candlesManager;
            _subscribersFactory = subscribersFactory;
            _connectionString = connectionString;
            _skipEodQuote = skipEodQuote;
        }

        private RabbitMqSubscriptionSettings _subscriptionSettings;
        public RabbitMqSubscriptionSettings SubscriptionSettings
        {
            get
            {
                if (_subscriptionSettings == null)
                {
                    _subscriptionSettings = RabbitMqSubscriptionSettingsHelper.GetSubscriptionSettings(_connectionString, "lykke.mt", "pricefeed");
                }
                return _subscriptionSettings;
            }
        }

        public void Start()
        {
            _subscriber = _subscribersFactory.Create<MtQuoteMessage>(SubscriptionSettings, ProcessQuoteAsync);
        }

        public void Stop()
        {
            _subscriber?.Stop();
        }

        private async Task ProcessQuoteAsync(MtQuoteMessage quote)
        {
            try
            {
                var validationErrors = ValidateQuote(quote);
                if (validationErrors.Any())
                {
                    var message = string.Join("\r\n", validationErrors);
                    await _log.WriteWarningAsync(nameof(MtQuotesSubscriber), nameof(ProcessQuoteAsync), quote.ToJson(), message);

                    return;
                }
                
                await _candlesManager.ProcessMtQuoteAsync(new MtQuoteDto
                {
                    AssetPair = quote.Instrument,
                    Ask = quote.Ask,
                    Bid = quote.Bid,
                    Timestamp = quote.Date
                });
            }
            catch (Exception)
            {
                await _log.WriteWarningAsync(nameof(MtQuotesSubscriber), nameof(ProcessQuoteAsync), quote.ToJson(), "Failed to process quote");
                throw;
            }
        }

        private IReadOnlyCollection<string> ValidateQuote(MtQuoteMessage quote)
        {
            var errors = new List<string>();

            if (quote == null)
            {
                errors.Add("Argument 'Order' is null.");
            }
            else
            {
                if (string.IsNullOrEmpty(quote.Instrument))
                {
                    errors.Add("Empty 'Instrument'");
                }
                if (quote.Date.Kind != DateTimeKind.Utc)
                {
                    errors.Add($"Invalid 'Date' Kind (UTC is required): '{quote.Date.Kind}'");
                }
                if (_skipEodQuote && (quote?.IsEod ?? false))
                {
                    errors.Add($"Skipping EOD quote");
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
