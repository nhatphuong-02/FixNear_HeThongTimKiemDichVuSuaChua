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
}
