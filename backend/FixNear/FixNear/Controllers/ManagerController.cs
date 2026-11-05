using FixNear.DTOs.Manager;
using FixNear.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FixNear.Controllers;

[ApiController]
[Route("api/manager")]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;

    public ManagerController(IManagerService managerService)
    {
        _managerService = managerService;
    }

    #region Xem thông tin cửa hàng

    [HttpGet("shops")]
    public async Task<IActionResult> GetShops(
        [FromQuery] int managerId)
    {
        if (managerId <= 0)
            return BadRequest(new
            {
                message = "ManagerId không hợp lệ"
            });

        var result = await _managerService
            .GetShopsAsync(managerId);

        return Ok(result);
    }

    [HttpGet("shops/{shopId:int}")]
    public async Task<IActionResult> GetShop(
        int shopId,
        [FromQuery] int managerId)
    {
        if (managerId <= 0 || shopId <= 0)
            return BadRequest(new
            {
                message = "ManagerId hoặc ShopId không hợp lệ"
            });

        var result = await _managerService
            .GetShopAsync(managerId, shopId);

        if (result == null)
            return NotFound(new
            {
                message = "Không tìm thấy cửa hàng"
            });

        return Ok(result);
    }

    #endregion

    #region Cập nhật thông tin cửa hàng

    [HttpPut("shops/{shopId:int}")]
    public async Task<IActionResult> UpdateShop(
        int shopId,
        [FromQuery] int managerId,
        [FromBody] UpdateShopDto dto)
    {
        if (managerId <= 0 || shopId <= 0)
            return BadRequest(new
            {
                message = "ManagerId hoặc ShopId không hợp lệ"
            });

        try
        {
            var updated = await _managerService
                .UpdateShopAsync(managerId, shopId, dto);

            if (!updated)
                return NotFound(new
                {
                    message = "Không tìm thấy cửa hàng"
                });

            return Ok(new
            {
                message = "Cập nhật cửa hàng thành công"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    #endregion

    #region Quản lý ảnh cửa hàng

    [HttpGet("shops/{shopId:int}/images")]
    public async Task<IActionResult> GetImages(
        int shopId,
        [FromQuery] int managerId)
    {
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
        [FromQuery] int managerId,
        [FromBody] ShopImageDto dto)
    {
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
        [FromQuery] int managerId,
        [FromBody] ShopImageDto dto)
    {
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
        int imageId,
        [FromQuery] int managerId)
    {
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
        int imageId,
        [FromQuery] int managerId)
    {
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

    #region Quan ly dich vu cua hang

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
        int shopId, [FromQuery] int managerId)
    {
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
        int shopId, int shopServiceId,
        [FromQuery] int managerId)
    {
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
        int shopId, [FromQuery] int managerId,
        [FromBody] AddShopServiceDto dto)
    {
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
        [FromQuery] int managerId,
        [FromBody] UpdateShopServiceDto dto)
    {
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
        int shopId, int shopServiceId,
        [FromQuery] int managerId)
    {
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
        int shopId, int shopServiceId,
        [FromQuery] int managerId)
    {
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

    #region Quan ly gia dich vu

    [HttpGet("service-prices")]
    public async Task<IActionResult> GetServicePrices(
        [FromQuery] int managerId,
        [FromQuery] int shopId,
        [FromQuery] bool includeHistory = false)
    {
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
        [FromQuery] int managerId,
        [FromQuery] int shopId)
    {
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
        [FromQuery] int managerId,
        [FromQuery] int shopId,
        [FromBody] ServicePriceDto dto)
    {
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
        [FromQuery] int managerId,
        [FromQuery] int shopId,
        [FromBody] UpdateServicePriceDto dto)
    {
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

    #region Quan ly ky thuat vien

    [HttpGet("technicians")]
    public async Task<IActionResult> GetTechnicians(
        [FromQuery] int managerId,
        [FromQuery] int shopId)
    {
        var data = await _managerService
            .GetTechniciansAsync(managerId, shopId);

        if (data == null)
            return NotFound(new
            {
                message = "Không tìm thấy cửa hàng thuộc Manager."
            });

        return Ok(new
        {
            message = "Lấy danh sách kỹ thuật viên thành công",
            data
        });
    }

    [HttpGet("technicians/{technicianId:int}")]
    public async Task<IActionResult> GetTechnician(
        int technicianId,
        [FromQuery] int managerId,
        [FromQuery] int shopId)
    {
        var data = await _managerService
            .GetTechnicianAsync(managerId, shopId, technicianId);

        if (data == null)
            return NotFound(new
            {
                message = "Không tìm thấy kỹ thuật viên đang thuộc cửa hàng."
            });

        return Ok(new
        {
            message = "Lấy chi tiết kỹ thuật viên thành công",
            data
        });
    }

    [HttpPost("technicians")]
    public async Task<IActionResult> AddTechnician(
        [FromQuery] int managerId,
        [FromQuery] int shopId,
        [FromBody] AddTechnicianToShopDto dto)
    {
        try
        {
            var data = await _managerService
                .AddTechnicianAsync(managerId, shopId, dto);

            if (data == null)
                return NotFound(new
                {
                    message = "Không tìm thấy cửa hàng thuộc Manager."
                });

            return StatusCode(201, new
            {
                message = "Thêm kỹ thuật viên vào cửa hàng thành công",
                data
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("technicians/{technicianId:int}")]
    public async Task<IActionResult> UpdateTechnician(
        int technicianId,
        [FromQuery] int managerId,
        [FromQuery] int shopId,
        [FromBody] UpdateTechnicianShopDto dto)
    {
        try
        {
            bool success = await _managerService
                .UpdateTechnicianAsync(
                    managerId, shopId, technicianId, dto);

            if (!success)
                return NotFound(new
                {
                    message = "Không tìm thấy kỹ thuật viên đang thuộc cửa hàng."
                });

            return Ok(new
            {
                message = "Cập nhật liên kết kỹ thuật viên thành công"
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("technicians/{technicianId:int}")]
    public async Task<IActionResult> RemoveTechnician(
        int technicianId,
        [FromQuery] int managerId,
        [FromQuery] int shopId)
    {
        bool success = await _managerService
            .RemoveTechnicianAsync(managerId, shopId, technicianId);

        if (!success)
            return NotFound(new
            {
                message = "Không tìm thấy kỹ thuật viên đang thuộc cửa hàng."
            });

        return Ok(new
        {
            message = "Đã cho kỹ thuật viên rời cửa hàng và lưu thời điểm LeftAt."
        });
    }

    #endregion



}
