// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;

using JetBrains.Annotations;

using Lykke.Job.CandlesProducer.Core.Domain.Quotes;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.CandlesProducer.Services.Quotes.Mt.Messages;
using Lykke.RabbitMqBroker.Subscriber;

using Microsoft.Extensions.Logging;

namespace Lykke.Job.CandlesProducer.Services.Quotes.Mt
{
    [UsedImplicitly]
    public class MtQuotesHandler : IMessageHandler<MtQuoteMessage>
    {
        private readonly ICandlesManager _candlesManager;
        private readonly ILogger<MtQuotesHandler> _logger;
        private readonly SkipEodQuote _skipEodQuote;

        public MtQuotesHandler(
            ICandlesManager candlesManager,
            ILogger<MtQuotesHandler> logger,
            SkipEodQuote skipEodQuote)
        {
            _candlesManager = candlesManager;
            _logger = logger;
            _skipEodQuote = skipEodQuote;
        }

        public async Task Handle(MtQuoteMessage message)
        {
            try
            {
                var validationErrors = ValidateQuote(message);
                if (validationErrors.Any())
                {
                    var errorsText = string.Join("\r\n", validationErrors);
                    _logger.LogWarning("Errors: {Errors}, quote: {Quote}", errorsText, message.ToJson());

                    return;
                }
                
                await _candlesManager.ProcessMtQuoteAsync(new MtQuoteDto
                {
                    AssetPair = message.Instrument,
                    Ask = message.Ask,
                    Bid = message.Bid,
                    Timestamp = message.Date
                });
            }
            catch (Exception)
            {
                _logger.LogWarning("Failed to process quote: {Quote}", message.ToJson());
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
                if (_skipEodQuote && (quote.IsEod ?? false))
                {
                    errors.Add($"Skipping EOD quote");
                }
            }

            return errors;
        }
    }
}
