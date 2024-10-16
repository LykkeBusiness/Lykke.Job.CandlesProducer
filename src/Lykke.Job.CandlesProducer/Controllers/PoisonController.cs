using System.Threading.Tasks;

using Lykke.Common.Api.Contract.Responses;
using Lykke.Job.CandlesProducer.Services;
using Lykke.Job.CandlesProducer.Services.Quotes;
using Lykke.Job.CandlesProducer.Services.Trades;

using Microsoft.AspNetCore.Mvc;

namespace Lykke.Job.CandlesProducer.Controllers;

[Route("api/[controller]")]
public class PoisonController : Controller
{
    [HttpPost("put-quotes-back")]
    public async Task<IActionResult> PutQuotesBack(
        [FromServices] IQuotesPoisonHandlingService service)
    {
        try
        {
            return Ok(await service.PutQuotesBack());
        }
        catch (ProcessAlreadyStartedException ex)
        {
            return Conflict(ErrorResponse.Create(ex.Message));
        }
    }

    [HttpPost("put-trades-back")]
    public async Task<IActionResult> PutTradesBack(
        [FromServices] ITradesPoisonHandlingService service)
    {
        try
        {
            return Ok(await service.PutTradesBack());
        }
        catch (ProcessAlreadyStartedException ex)
        {
            return Conflict(ErrorResponse.Create(ex.Message));
        }
    }
}
