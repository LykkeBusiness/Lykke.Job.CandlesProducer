// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using CorporateActions.Broker.Contracts.Workflow;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Lykke.Job.CandlesProducer.Contract;
using Lykke.Job.CandlesProducer.Core.Services.Assets;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Microsoft.Extensions.Logging;

namespace Lykke.Job.CandlesProducer.Workflow
{
    public class RFactorCommandsHandler
    {
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly ICandlesManager _candlesManager;
        private readonly ILogger<RFactorCommandsHandler> _logger;

        public RFactorCommandsHandler(IAssetPairsManager assetPairsManager,
            ICandlesManager candlesManager,
            ILogger<RFactorCommandsHandler> logger)
        {
            _assetPairsManager = assetPairsManager;
            _candlesManager = candlesManager;
            _logger = logger;
        }

        [UsedImplicitly]
        public async Task Handle(UpdateCurrentCandlesCommand command, IEventPublisher publisher)
        {
            _logger.LogInformation("{Command} received for product {ProductId}",
                nameof(UpdateCurrentCandlesCommand),
                command.ProductId);

            var asset = await _assetPairsManager.TryGetEnabledPairAsync(command.ProductId);

            if (asset == null)
            {
                throw new Exception(
                    $"Product {command.ProductId}. Cannot update current candles for rFactor task with id {command.TaskId}");
            }

            if (command.RFactorDate.Date == command.LastTradingDay.Date)
            {
                _logger.LogInformation("Updating RFactor for product {Product}, RFactorDate == LastTradingDay", command.ProductId);
                await _candlesManager.UpdateRFactor(command.ProductId, decimal.ToDouble(command.RFactor));
            }
            else
            {
                if (command.RFactorDate.SameWeek(command.LastTradingDay, DayOfWeek.Monday))
                {
                    _logger.LogInformation("Updating RFactor for product {Product}, RFactorDate same >week< as LastTradingDay", command.ProductId);
                    await _candlesManager.UpdateRFactor(command.ProductId,
                        decimal.ToDouble(command.RFactor),
                        CandleTimeInterval.Week);
                }

                if (command.RFactorDate.SameMonth(command.LastTradingDay))
                {
                    _logger.LogInformation("Updating RFactor for product {Product}, RFactorDate same >month< as LastTradingDay", command.ProductId);
                    await _candlesManager.UpdateRFactor(command.ProductId,
                        decimal.ToDouble(command.RFactor),
                        CandleTimeInterval.Month);
                }
            }

            publisher.PublishEvent(new CurrentCandlesUpdatedEvent() { TaskId = command.TaskId, });
        }
    }
}
