using System.Collections.Generic;

using Lykke.Contracts.Responses;

namespace Lykke.Job.CandlesProducer.Contract.Candles;

public class GetCandlesResponse : ErrorCodeResponse<CandlesErrorCodesContract>
{
    public List<CandleContract> Candles { get; set; }
}