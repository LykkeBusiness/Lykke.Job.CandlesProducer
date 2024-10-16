using System.Threading.Tasks;

namespace Lykke.Job.CandlesProducer.Services.Quotes;

public interface IQuotesPoisonHandlingService
{
    Task<string> PutQuotesBack();
}
