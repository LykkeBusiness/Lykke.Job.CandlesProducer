using System.Threading.Tasks;

using Refit;

namespace Lykke.Job.CandlesProducer.Contract.Candles;

public interface ICandlesApi
{
    [Get("/api/candles/{productId}")]
    Task<GetCandlesResponse> Get(string productId);

    [Post("/api/candles")]
    Task UpsertCandle(UpsertCandleRequest request);
}