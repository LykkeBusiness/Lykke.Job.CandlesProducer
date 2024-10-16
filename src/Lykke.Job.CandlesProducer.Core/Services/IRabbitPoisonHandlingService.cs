using System.Threading.Tasks;

namespace Lykke.Job.CandlesProducer.Core.Services
{
    public interface IRabbitPoisonHandlingService
    {
        Task<string> PutMessagesBack();
    }
}
