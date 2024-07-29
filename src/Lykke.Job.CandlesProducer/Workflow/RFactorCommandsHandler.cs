// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using CorporateActions.Broker.Contracts.Workflow;
using JetBrains.Annotations;
using Lykke.Cqrs;
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
                _logger.LogWarning("Product {ProductId is not found}. RFactor saga with id {Id} will be stopped",
                    command.ProductId,
                    command.TaskId);
                return;
            }

            if (command.RFactorDate == command.LastTradingDay)
            {
                await _candlesManager.UpdateRFactor(command.ProductId, decimal.ToDouble(command.RFactor));
            }
            else
            {
                await _candlesManager.UpdateMonthlyOrWeeklyRFactor(command.ProductId, decimal.ToDouble(command.RFactor),
                    command.RFactorDate, command.LastTradingDay);
            }

            publisher.PublishEvent(new CurrentCandlesUpdatedEvent() { TaskId = command.TaskId, });
        }
    }
}
