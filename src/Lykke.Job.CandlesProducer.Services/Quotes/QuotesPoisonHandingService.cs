using Lykke.RabbitMqBroker;

namespace Lykke.Job.CandlesProducer.Services.Quotes;

public class QuotesPoisonHandlingService(IPoisonQueueHandler poisonQueueHandler) : IQuotesPoisonHandlingService
{
    private readonly IPoisonQueueHandler _poisonQueueHandler = poisonQueueHandler;

    public string PutQuotesBack() => _poisonQueueHandler.TryPutMessagesBack();
}
