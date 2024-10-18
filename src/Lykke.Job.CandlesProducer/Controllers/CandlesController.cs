// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Lykke.Contracts.Responses;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Job.CandlesProducer.Contract.Candles;
using Lykke.Job.CandlesProducer.Core.Domain.Candles;
using Lykke.Job.CandlesProducer.Core.Services;
using Lykke.Job.CandlesProducer.Core.Services.Assets;
using Lykke.Job.CandlesProducer.Core.Services.Candles;

using Microsoft.AspNetCore.Mvc;

namespace Lykke.Job.CandlesProducer.Controllers;

[Route("api/[controller]")]
public class CandlesController : ControllerBase, ICandlesApi
{
    private readonly ICandlesGenerator _candlesGenerator;
    private readonly IAssetPairsManager _assetPairsManager;
    private readonly IHaveState<ImmutableDictionary<string, ICandle>> _candlesState;

    public CandlesController(
        ICandlesGenerator candlesGenerator,
        IAssetPairsManager assetPairsManager,
        IHaveState<ImmutableDictionary<string, ICandle>> candlesState)
    {
        _candlesGenerator = candlesGenerator;
        _assetPairsManager = assetPairsManager;
        _candlesState = candlesState;
    }

    [HttpGet("{productId}")]
    public async Task<GetCandlesResponse> Get(string productId)
    {
        var state = _candlesState.GetState();
        var candles = state.Values
            .Where(x => x.AssetPairId == productId)
            .Select(Map)
            .ToList();

        return candles.Count == 0
            ? new GetCandlesResponse { ErrorCode = CandlesErrorCodesContract.NotFound }
            : new GetCandlesResponse { Candles = candles };
    }

    [HttpPost]
    public async Task<ErrorCodeResponse<CandlesErrorCodesContract>> UpsertCandle([FromBody] UpsertCandleRequest request)
    {
        var validationResult = await Validate(request);
        if (validationResult != CandlesErrorCodesContract.None) return validationResult;

        _candlesGenerator.UpsertCandle(
            request.ProductId,
            request.Timestamp,
            request.Open,
            request.Close,
            request.Low,
            request.High,
            request.PriceType,
            request.TimeInterval);

        return CandlesErrorCodesContract.None;
    }

    private async Task<CandlesErrorCodesContract> Validate(UpsertCandleRequest request)
    {
        if (request.Low > request.High)
        {
            return CandlesErrorCodesContract.InvalidLowOrHighPrice;
        }

        var assetPair = await _assetPairsManager.TryGetEnabledPairAsync(request.ProductId.Trim());

        if (assetPair == null)
        {
            return CandlesErrorCodesContract.ProductNotFound;
        }

        if (request.PriceType == CandlePriceType.Unspecified || request.PriceType == CandlePriceType.Trades)
        {
            return CandlesErrorCodesContract.PriceTypeNotSupported;
        }

        if (request.TimeInterval == CandleTimeInterval.Unspecified)
        {
            return CandlesErrorCodesContract.TimeIntervalNotSupported;
        }

        return CandlesErrorCodesContract.None;
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