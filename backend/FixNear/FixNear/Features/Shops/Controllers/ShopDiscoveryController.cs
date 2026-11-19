using FixNear.Features.Shops.DTOs;
using FixNear.Features.Shops.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Features.Shops.Controllers
{
    [ApiController]
    [Route("api/services/{serviceId:int}/shops")]
    public class ShopDiscoveryController : ControllerBase
    {
        private readonly IShopService _shopService;

        public ShopDiscoveryController(IShopService shopService)
        {
            _shopService = shopService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNearbyShops(
            int serviceId,
            [FromQuery] NearbyShopRequestDto request)
        {
            try
            {
                var result = await _shopService.GetNearbyShopsByServiceAsync(
                    serviceId,
                    request);

                return Ok(result);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }
    }
}