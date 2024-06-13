using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Services.Quotes;
using Lykke.Job.CandlesProducer.Services.Trades;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Job.CandlesProducer.Controllers
{
    [Route("api/[controller]")]
    public class PoisonController : Controller
    {
        private readonly IQuotesPoisonHandingService _quotesPoisonHandingService;
        private readonly ITradesPoisonHandingService _tradesPoisonHandingService;

        public PoisonController(
            IQuotesPoisonHandingService quotesPoisonHandingService,
            ITradesPoisonHandingService tradesPoisonHandingService)
        {
            _quotesPoisonHandingService = quotesPoisonHandingService;
            _tradesPoisonHandingService = tradesPoisonHandingService;
        }

        [HttpPost("put-quotes-back")]
        public async Task<string> PutQuotesBack()
        {
            return await _quotesPoisonHandingService.PutQuotesBack();
        }

        [HttpPost("put-trades-back")]
        public async Task<string> PutTradesBack()
        {
            return await _tradesPoisonHandingService.PutTradesBack();
        }
    }
}
