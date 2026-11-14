using FixNear.Features.ServiceCatalog.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Features.ServiceCatalog.Controllers
{
    [ApiController]
    [Route("api/service-categories")]
    public class ServiceCategoryController : ControllerBase
    {
        private readonly IServiceCategoryService _service;

        public ServiceCategoryController(IServiceCategoryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoriesAsync()
        {
            var result = await _service.GetCategoriesAsync();
            
            return Ok(result);
        }

        [HttpGet("{categoryId:int}")]
        public async Task<IActionResult> GetCategoryByIdAsync(int categoryId)
        {
            try
            {
                var result = await _service.GetCategoryByIdAsync(categoryId);
                return Ok(result);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }
    }
}
