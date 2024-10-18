using Lykke.RabbitMqBroker;

namespace Lykke.Job.CandlesProducer.Services.Trades;

public class TradesPoisonHandlingService(IPoisonQueueHandler poisonQueueHandler) : ITradesPoisonHandlingService
{
    private readonly IPoisonQueueHandler _poisonQueueHandler = poisonQueueHandler;

    public string PutTradesBack() => _poisonQueueHandler.TryPutMessagesBack();
}
