using System.Threading.Tasks;

namespace Lykke.Job.CandlesProducer.Core.Services
{
    public interface IRabbitPoisonHandingService
    {
        Task<string> PutMessagesBack();
    }
}
