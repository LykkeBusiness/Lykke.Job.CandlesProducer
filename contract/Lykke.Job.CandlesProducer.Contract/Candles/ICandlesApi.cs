using System.Threading.Tasks;

using Lykke.Contracts.Responses;

using Refit;

namespace Lykke.Job.CandlesProducer.Contract.Candles;

public interface ICandlesApi
{
    [Get("/api/candles/{productId}")]
    Task<GetCandlesResponse> Get(string productId);

    [Post("/api/candles")]
    Task<ErrorCodeResponse<CandlesErrorCodesContract>> UpsertCandle(UpsertCandleRequest request);
}