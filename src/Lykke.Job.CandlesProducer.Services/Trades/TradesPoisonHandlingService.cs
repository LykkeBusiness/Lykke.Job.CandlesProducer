using System.Threading.Tasks;

using Lykke.Job.CandlesProducer.Core.Services;

namespace Lykke.Job.CandlesProducer.Services.Trades;

public class TradesPoisonHandlingService : ITradesPoisonHandlingService
{
    private readonly IRabbitPoisonHandlingService _rabbitPoisonHandingService;

    public TradesPoisonHandlingService(IRabbitPoisonHandlingService rabbitPoisonHandingService)
    {
        _rabbitPoisonHandingService = rabbitPoisonHandingService;
    }

    public async Task<string> PutTradesBack()
    {
        return await _rabbitPoisonHandingService.PutMessagesBack();
    }
}
