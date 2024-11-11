using System.Threading.Tasks;

namespace Lykke.Job.CandlesProducer.Services.Trades;

public interface ITradesPoisonHandlingService
{
    string PutTradesBack();
}
