using FixNear.Features.RepairRequest.DTOs;
using FixNear.Features.RepairRequest.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Features.RepairRequest.Controllers
{
    [ApiController]
    [Route("api/repair-items")]
    [Authorize(Roles = "4")]
    public class RepairItemController : ControllerBase
    {
        private readonly IRepairItemService _repairItemService;

        public RepairItemController(
            IRepairItemService repairItemService)
        {
            _repairItemService = repairItemService;
        }

        // ============================================
        // TAO DANH MUC HANG MUC SUA CHUA
        // POST /api/repair-items
        // ============================================

        [HttpPost]
        public async Task<IActionResult> CreateRepairItem(
            [FromBody] CreateRepairItemDto dto)
        {
            // 1. Goi Service tao hang muc
            var result = await _repairItemService.CreateAsync(dto);

            // 2. Xu ly truong hop tao that bai
            if (!result.Success)
            {
                return Conflict(new
                {
                    message = result.Message
                });
            }

            // 3. Tra ve ket qua khi tao thanh cong
            return StatusCode(201, new
            {
                message = result.Message,
                data = new
                {
                    repairItemId = result.RepairItemId
                }
            });
        }
    }
}
