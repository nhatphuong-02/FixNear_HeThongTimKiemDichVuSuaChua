using FixNear.Features.ServiceCatalog.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Features.ServiceCatalog.Controllers
{
    [ApiController]
    [Route("api/services")]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceService _service;

        public ServiceController(IServiceService service)
        {
            _service = service;
        }

        // GET: api/services
        [HttpGet]
        public async Task<IActionResult> GetServicesAsync()
        {
            var result = await _service.GetServicesAsync();
            return Ok(result);
        }

        // GET: api/services/{serviceId}
        [HttpGet("{serviceId:int}")]
        public async Task<IActionResult> GetServiceByIdAsync(int serviceId)
        {
            try
            {
                var result = await _service.GetServiceByIdAsync(serviceId);
                return Ok(result);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }
    }
}