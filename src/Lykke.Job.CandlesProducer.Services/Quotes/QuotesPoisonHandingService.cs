using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Core.Services;

namespace Lykke.Job.CandlesProducer.Services.Quotes
{
    public class QuotesPoisonHandingService : IQuotesPoisonHandingService
    {
        private readonly IRabbitPoisonHandingService _rabbitPoisonHandingService;

        public QuotesPoisonHandingService(IRabbitPoisonHandingService rabbitPoisonHandingService)
        {
            _rabbitPoisonHandingService = rabbitPoisonHandingService;
        }

        public async Task<string> PutQuotesBack()
        {
            return await _rabbitPoisonHandingService.PutMessagesBack();
        }
    }
}
