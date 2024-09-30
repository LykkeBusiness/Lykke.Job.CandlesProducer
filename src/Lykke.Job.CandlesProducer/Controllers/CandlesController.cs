// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Lykke.Job.CandlesProducer.Contract.Candles;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Candles;

using Microsoft.AspNetCore.Mvc;

namespace Lykke.Job.CandlesProducer.Controllers;

[Route("api/[controller]")]
public class CandlesController : ControllerBase, ICandlesApi
{
    private readonly ICandlesGenerator _candlesGenerator;
    private readonly IHaveState<ImmutableDictionary<string, ICandle>> _candlesState;

    public CandlesController(
        ICandlesGenerator candlesGenerator,
        IHaveState<ImmutableDictionary<string, ICandle>> candlesState)
    {
        _candlesGenerator = candlesGenerator;
        _candlesState = candlesState;
    }

    [HttpGet("{productId}")]
    public async Task<GetCandlesResponse> Get(string productId)
    {
        var state = _candlesState.GetState();
        var keys = state.Keys.Where(x => x.StartsWith($"{productId}-"));
        var candles = state
            .Where(x => keys.Contains(x.Key))
            .Select(x => x.Value)
            .Select(Map)
            .ToList();

        return candles.Count == 0
            ? new GetCandlesResponse { ErrorCode = CandlesErrorCodesContract.NotFound }
            : new GetCandlesResponse { Candles = candles };
    }

    [HttpPost]
    public Task UpsertCandle(UpsertCandleRequest request)
    {
        _candlesGenerator.UpdateQuotingCandle(
            request.ProductId,
            request.Timestamp,
            request.Price,
            request.PriceType,
            request.TimeInterval);

        return Task.CompletedTask;
    }

    private static CandleContract Map(ICandle candle)
    {
        return new CandleContract()
        {
            AssetPairId = candle.AssetPairId,
            PriceType = candle.PriceType,
            TimeInterval = candle.TimeInterval,
            Timestamp = candle.Timestamp,
            Open = candle.Open,
            Close = candle.Close,
            High = candle.High,
            Low = candle.Low,
            TradingVolume = candle.TradingVolume,
            TradingOppositeVolume = candle.TradingOppositeVolume,
            LatestChangeTimestamp = candle.LatestChangeTimestamp,
            OpenTimestamp = candle.OpenTimestamp,
        };
    }
}