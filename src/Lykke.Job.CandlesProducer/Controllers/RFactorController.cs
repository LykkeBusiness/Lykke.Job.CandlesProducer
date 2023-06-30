// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Threading.Tasks;
using Lykke.Job.CandlesProducer.Core.Services.Assets;
using Lykke.Job.CandlesProducer.Core.Services.Candles;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Job.CandlesProducer.Controllers
{
    [Route("api/[controller]")]
    public class RFactorController : Controller
    {
        private readonly IAssetPairsManager _assetPairsManager;
        private readonly ICandlesManager _candlesManager;

        public RFactorController(IAssetPairsManager assetPairsManager, ICandlesManager candlesManager)
        {
            _assetPairsManager = assetPairsManager;
            _candlesManager = candlesManager;
        }

        [HttpPut("update")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> UpdateRFactor(string assetPair, double rFactor)
        {
            var asset = await _assetPairsManager.TryGetEnabledPairAsync(assetPair);

            if (asset == null)
            {
                return NotFound();
            }
            
            await _candlesManager.UpdateRFactor(assetPair, rFactor);

            return Ok();
        }
    }
}
