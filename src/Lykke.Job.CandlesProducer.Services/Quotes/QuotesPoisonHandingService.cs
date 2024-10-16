﻿using System.Threading.Tasks;

using Lykke.Job.CandlesProducer.Core.Services;

namespace Lykke.Job.CandlesProducer.Services.Quotes;

public class QuotesPoisonHandlingService : IQuotesPoisonHandlingService
{
    private readonly IRabbitPoisonHandlingService _rabbitPoisonHandingService;

    public QuotesPoisonHandlingService(IRabbitPoisonHandlingService rabbitPoisonHandingService)
    {
        _rabbitPoisonHandingService = rabbitPoisonHandingService;
    }

    public async Task<string> PutQuotesBack()
    {
        return await _rabbitPoisonHandingService.PutMessagesBack();
    }
}
