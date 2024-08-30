using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Core.Services;

namespace Lykke.Job.CandlesProducer.Services.Trades
{
    public class TradesPoisonHandingService : ITradesPoisonHandingService
    {
        private readonly IRabbitPoisonHandingService _rabbitPoisonHandingService;

        public TradesPoisonHandingService(IRabbitPoisonHandingService rabbitPoisonHandingService)
        {
            _rabbitPoisonHandingService = rabbitPoisonHandingService;
        }

        public async Task<string> PutTradesBack()
        {
            return await _rabbitPoisonHandingService.PutMessagesBack();
        }
    }
}
