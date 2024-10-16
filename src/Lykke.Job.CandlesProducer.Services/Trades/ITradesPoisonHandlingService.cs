using System.Threading.Tasks;

namespace Lykke.Job.CandlesProducer.Services.Trades;

public interface ITradesPoisonHandlingService
{
    Task<string> PutTradesBack();
}
