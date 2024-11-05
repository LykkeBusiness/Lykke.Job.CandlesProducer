using System.Net;

using Lykke.Common.Api.Contract.Responses;
using Lykke.Job.CandlesProducer.Services.Quotes;
using Lykke.Job.CandlesProducer.Services.Trades;
using Lykke.RabbitMqBroker;

using Microsoft.AspNetCore.Mvc;

namespace Lykke.Job.CandlesProducer.Controllers;

[Route("api/[controller]")]
public class PoisonController : Controller
{
    [HttpPost("put-quotes-back")]
    public IActionResult PutQuotesBack([FromServices] IQuotesPoisonHandlingService service)
    {
        try
        {
            var text = service.PutQuotesBack();
            return string.IsNullOrEmpty(text)
                ? NotFound(ErrorResponse.Create("There are no messages to put back"))
                : Ok(text);
        }
        catch (ProcessAlreadyStartedException ex)
        {
            return Conflict(ErrorResponse.Create(ex.Message));
        }
        catch (LockAcqTimeoutException ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, ErrorResponse.Create(ex.Message));
        }
    }

    [HttpPost("put-trades-back")]
    public IActionResult PutTradesBack([FromServices] ITradesPoisonHandlingService service)
    {
        try
        {
            var text = service.PutTradesBack();
            return string.IsNullOrEmpty(text)
                ? NotFound(ErrorResponse.Create("There are no messages to put back"))
                : Ok(text);
        }
        catch (ProcessAlreadyStartedException ex)
        {
            return Conflict(ErrorResponse.Create(ex.Message));
        }
        catch (LockAcqTimeoutException ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, ErrorResponse.Create(ex.Message));
        }
    }
}
