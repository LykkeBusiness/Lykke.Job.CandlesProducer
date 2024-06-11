// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;

using JetBrains.Annotations;

using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Lykke.Job.QuotesProducer.Contract;
using Lykke.RabbitMqBroker.Subscriber;

using Microsoft.Extensions.Logging;

namespace Lykke.Job.CandlesProducer.Services.Quotes.Spot
{
    [UsedImplicitly]
    public class SpotQuotesHandler : IMessageHandler<QuoteMessage>
    {
        private readonly ICandlesManager _candlesManager;
        private readonly ILogger<SpotQuotesHandler> _logger;

        public SpotQuotesHandler(ICandlesManager candlesManager, ILogger<SpotQuotesHandler> logger)
        {
            _candlesManager = candlesManager;
            _logger = logger;
        }

        public async Task Handle(QuoteMessage message)
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

                await _candlesManager.ProcessSpotQuoteAsync(message);
            }
            catch (Exception)
            {
                _logger.LogWarning("Failed to process quote : {Quote}", message.ToJson());
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
    }
}
