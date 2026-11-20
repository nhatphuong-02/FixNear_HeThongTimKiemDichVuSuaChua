using FixNear.Features.Manager.DTOs;
using FixNear.Features.Manager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FixNear.Features.Manager.Controllers;

[ApiController]
[Route("api/manager")]
[Authorize(Roles = "2")]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;

    public ManagerController(IManagerService managerService)
    {
        _managerService = managerService;
    }

    //Lấy id người dùng hiện tại
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(claim, out int userId)
            ? userId
            : null;
    }


    #region T03 - Quản lý ảnh cửa hàng

    [HttpGet("shops/{shopId:int}/images")]
    public async Task<IActionResult> GetImages(
        int shopId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        if (managerId <= 0 || shopId <= 0)
            return BadRequest(new
            {
                message = "ManagerId hoặc ShopId không hợp lệ"
            });

        var result = await _managerService
            .GetShopImagesAsync(managerId, shopId);

        return Ok(result);
    }

    [HttpPost("shops/{shopId:int}/images")]
    public async Task<IActionResult> AddImage(
        int shopId,
        [FromBody] ShopImageDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        if (managerId <= 0 || shopId <= 0)
            return BadRequest(new
            {
                message = "ManagerId hoặc ShopId không hợp lệ"
            });

        try
        {
            var result = await _managerService
                .AddShopImageAsync(managerId, shopId, dto);

            if (result == null)
                return NotFound(new
                {
                    message = "Không tìm thấy cửa hàng"
                });

            return Ok(new
            {
                message = "Thêm ảnh thành công",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPut("shops/{shopId:int}/images/{imageId:int}")]
    public async Task<IActionResult> UpdateImage(
        int shopId,
        int imageId,
        [FromBody] ShopImageDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        if (managerId <= 0 || shopId <= 0 || imageId <= 0)
            return BadRequest(new
            {
                message = "ID không hợp lệ"
            });

        var updated = await _managerService
            .UpdateShopImageAsync(
                managerId,
                shopId,
                imageId,
                dto);

        if (!updated)
            return NotFound(new
            {
                message = "Không tìm thấy ảnh thuộc cửa hàng"
            });

        return Ok(new
        {
            message = "Cập nhật ảnh thành công"
        });
    }

    [HttpDelete("shops/{shopId:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(
        int shopId,
        int imageId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        if (managerId <= 0 || shopId <= 0 || imageId <= 0)
            return BadRequest(new
            {
                message = "ID không hợp lệ"
            });

        var deleted = await _managerService
            .DeleteShopImageAsync(
                managerId,
                shopId,
                imageId);

        if (!deleted)
            return NotFound(new
            {
                message = "Không tìm thấy ảnh thuộc cửa hàng"
            });

        return Ok(new
        {
            message = "Xóa ảnh thành công"
        });
    }

    [HttpPut("shops/{shopId:int}/images/{imageId:int}/primary")]
    public async Task<IActionResult> SetPrimaryImage(
        int shopId,
        int imageId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        if (managerId <= 0 || shopId <= 0 || imageId <= 0)
            return BadRequest(new
            {
                message = "ID không hợp lệ"
            });

        var updated = await _managerService
            .SetPrimaryImageAsync(
                managerId,
                shopId,
                imageId);

        if (!updated)
            return NotFound(new
            {
                message = "Không tìm thấy ảnh thuộc cửa hàng"
            });

        return Ok(new
        {
            message = "Đặt ảnh chính thành công"
        });
    }

    #endregion

    #region T04 - Quan ly dich vu cua hang

    [HttpGet("services")]
    public async Task<IActionResult> GetSystemServices()
    {
        var result = await _managerService.GetSystemServicesAsync();
        return Ok(new
        {
            message = "Lấy danh mục dịch vụ thành công",
            data = result
        });
    }

    [HttpGet("shops/{shopId}/services")]
    public async Task<IActionResult> GetShopServices(
        int shopId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        var result = await _managerService
            .GetShopServicesAsync(managerId, shopId);

        if (result == null)
            return NotFound(new
            {
                message = "Không tìm thấy cửa hàng thuộc Manager"
            });

        return Ok(new
        {
            message = "Lấy danh sách dịch vụ thành công",
            data = result
        });
    }

    [HttpGet("shops/{shopId}/services/{shopServiceId}")]
    public async Task<IActionResult> GetShopServiceDetail(
        int shopId, int shopServiceId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        var result = await _managerService
            .GetShopServiceDetailAsync(
                managerId, shopId, shopServiceId);

        if (result == null)
            return NotFound(new
            {
                message = "Không tìm thấy dịch vụ của cửa hàng"
            });

        return Ok(new
        {
            message = "Lấy chi tiết dịch vụ thành công",
            data = result
        });
    }

    [HttpPost("shops/{shopId}/services")]
    public async Task<IActionResult> AddShopService(
        int shopId, [FromBody] AddShopServiceDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        try
        {
            var result = await _managerService
                .AddShopServiceAsync(managerId, shopId, dto);

            if (result == null)
                return NotFound(new
                {
                    message = "Không tìm thấy cửa hàng thuộc Manager"
                });

            return Ok(new
            {
                message = "Thêm dịch vụ thành công",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("shops/{shopId}/services/{shopServiceId}")]
    public async Task<IActionResult> UpdateShopService(
        int shopId, int shopServiceId,
        [FromBody] UpdateShopServiceDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        var success = await _managerService
            .UpdateShopServiceAsync(
                managerId, shopId, shopServiceId, dto);

        if (!success)
            return NotFound(new
            {
                message = "Không tìm thấy dịch vụ của cửa hàng"
            });

        return Ok(new
        {
            message = "Cập nhật dịch vụ thành công"
        });
    }

    [HttpPatch("shops/{shopId}/services/{shopServiceId}/deactivate")]
    public async Task<IActionResult> DeactivateShopService(
        int shopId, int shopServiceId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        var success = await _managerService
            .SetShopServiceStatusAsync(
                managerId, shopId, shopServiceId, false);

        if (!success)
            return NotFound(new
            {
                message = "Không tìm thấy dịch vụ của cửa hàng"
            });

        return Ok(new
        {
            message = "Đã ngừng cung cấp dịch vụ"
        });
    }

    [HttpPatch("shops/{shopId}/services/{shopServiceId}/activate")]
    public async Task<IActionResult> ActivateShopService(
        int shopId, int shopServiceId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        try
        {
            var success = await _managerService
                .SetShopServiceStatusAsync(
                    managerId, shopId, shopServiceId, true);

            if (!success)
                return NotFound(new
                {
                    message = "Không tìm thấy dịch vụ của cửa hàng"
                });

            return Ok(new
            {
                message = "Đã kích hoạt lại dịch vụ"
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    #endregion

    #region T05 - Quan ly gia dich vu

    [HttpGet("service-prices")]
    public async Task<IActionResult> GetServicePrices(
        [FromQuery] int shopId,
        [FromQuery] bool includeHistory = false)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        var data = await _managerService
            .GetServicePricesAsync(managerId, shopId, includeHistory);

        if (data == null)
            return NotFound(new
            {
                message = "Không tìm thấy cửa hàng thuộc Manager."
            });

        return Ok(new
        {
            message = "Lấy danh sách giá thành công",
            data
        });
    }

    [HttpGet("service-prices/{servicePriceId:int}")]
    public async Task<IActionResult> GetServicePrice(
        int servicePriceId,
        [FromQuery] int shopId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        var data = await _managerService
            .GetServicePriceAsync(managerId, shopId, servicePriceId);

        if (data == null)
            return NotFound(new
            {
                message = "Không tìm thấy giá thuộc cửa hàng."
            });

        return Ok(new
        {
            message = "Lấy chi tiết giá thành công",
            data
        });
    }

    [HttpPost("service-prices")]
    public async Task<IActionResult> AddServicePrice(
        [FromQuery] int shopId,
        [FromBody] ServicePriceDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        try
        {
            var data = await _managerService
                .AddServicePriceAsync(managerId, shopId, dto);

            if (data == null)
                return NotFound(new
                {
                    message = "Không tìm thấy dịch vụ thuộc cửa hàng của Manager."
                });

            return StatusCode(201, new
            {
                message = "Thêm giá dịch vụ thành công",
                data
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("service-prices/{servicePriceId:int}")]
    public async Task<IActionResult> UpdateServicePrice(
        int servicePriceId,
        [FromQuery] int shopId,
        [FromBody] UpdateServicePriceDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        try
        {
            var data = await _managerService
                .UpdateServicePriceAsync(
                    managerId, shopId, servicePriceId, dto);

            if (data == null)
                return NotFound(new
                {
                    message = "Không tìm thấy giá hiện hành thuộc cửa hàng."
                });

            return Ok(new
            {
                message = "Cập nhật giá và lưu lịch sử thành công",
                data
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    #endregion

    #region Danh sách và chi tiết Repair

    [HttpGet("repair-requests")]
    public async Task<IActionResult> GetRepairRequests(
        [FromQuery] int shopId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (shopId <= 0)
            return BadRequest(new { message = "ShopId không hợp lệ." });

        try
        {
            var result = await _managerService
                .GetRepairRequestsAsync(userId.Value, shopId);

            return Ok(new
            {
                message = "Lấy danh sách yêu cầu thành công.",
                data = result
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("repair-requests/{id:int}")]
    public async Task<IActionResult> GetRepairRequestDetail(
        int id,
        [FromQuery] int shopId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (shopId <= 0 || id <= 0)
            return BadRequest(new { message = "ID không hợp lệ." });

        try
        {
            var result = await _managerService
                .GetRepairRequestDetailAsync(
                    userId.Value, shopId, id);

            if (result == null)
                return NotFound(new
                {
                    message = "Không tìm thấy yêu cầu sửa chữa."
                });

            return Ok(new
            {
                message = "Lấy chi tiết thành công.",
                data = result
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    #endregion

    #region Tiếp nhận Repair

    [HttpPatch("repair-requests/{id:int}/accept")]
    public async Task<IActionResult> AcceptRepairRequest(
        int id,
        [FromQuery] int shopId,
        [FromBody] AcceptRepairRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (shopId <= 0 || id <= 0)
            return BadRequest(new { message = "ID không hợp lệ." });

        try
        {
            var result = await _managerService
                .AcceptRepairRequestAsync(
                    userId.Value, shopId, id, dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    #endregion

    #region Từ chối Repair

    [HttpPatch("repair-requests/{id:int}/reject")]
    public async Task<IActionResult> RejectRepairRequest(
        int id,
        [FromQuery] int shopId,
        [FromBody] RejectRepairRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (shopId <= 0 || id <= 0)
            return BadRequest(new { message = "ID không hợp lệ." });

        try
        {
            var result = await _managerService
                .RejectRepairRequestAsync(
                    userId.Value, shopId, id, dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    #endregion


    #region T10 - Phan cong ky thuat vien

    [HttpPost("repair-assignments")]
    public async Task<IActionResult> AssignRepairTechnician(
        [FromQuery] int shopId,
        [FromBody] AssignTechnicianDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        try
        {
            var result = await _managerService
                .AssignRepairTechnicianAsync(
                    managerId,
                    shopId,
                    dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Không thể tạo phân công do ràng buộc dữ liệu."
            });
        }
    }

    #endregion

    #region T11 - Doi ky thuat vien

    [HttpPatch("repair-assignments/{id:int}/reassign")]
    public async Task<IActionResult> ReassignRepairTechnician(
        int id,
        [FromQuery] int shopId,
        [FromBody] ReassignTechnicianDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        try
        {
            var result = await _managerService
                .ReassignRepairTechnicianAsync(
                    managerId,
                    shopId,
                    id,
                    dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Không thể đổi kỹ thuật viên do ràng buộc dữ liệu."
            });
        }
    }

    #endregion

    #region T12 - Xem va tao lich sua chua

    [HttpGet("repair-schedules")]
    public async Task<IActionResult> GetManagerRepairSchedules(
        [FromQuery] int shopId,
        [FromQuery] bool includeHistory = false)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        var result = await _managerService
            .GetManagerRepairSchedulesAsync(
                managerId,
                shopId,
                includeHistory);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy cửa hàng thuộc Manager."
            });
        }

        return Ok(new
        {
            message = "Lấy danh sách lịch sửa chữa thành công.",
            data = result
        });
    }

    [HttpPost("repair-schedules")]
    public async Task<IActionResult> CreateManagerRepairSchedule(
        [FromQuery] int shopId,
        [FromBody] CreateRepairScheduleDto dto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue) return Unauthorized();
        int managerId = currentUserId.Value;

        try
        {
            var result = await _managerService
                .CreateManagerRepairScheduleAsync(
                    managerId,
                    shopId,
                    dto);

            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                data = result.Data
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "Không thể tạo lịch do trùng lịch hoặc ràng buộc dữ liệu."
            });
        }
    }

    #endregion


}

// Converts failed ShopMember authorization to HTTP 403 for older Manager actions.
public sealed class ManagerShopAuthorizationExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is UnauthorizedAccessException)
        {
            context.Result = new ForbidResult();
            context.ExceptionHandled = true;
        }
    }
}
