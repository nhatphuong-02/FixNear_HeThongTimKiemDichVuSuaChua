
using FixNear.Features.ServiceCatalog.DTOs;
using FixNear.Features.ServiceCatalog.Interfaces;
using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.ServiceCatalog.Controllers;

[ApiController]
[Route("api/services")]
public class ServiceController : ControllerBase
{
    private readonly IServiceService _service;
    private readonly FixNearDbContext _db;

    public ServiceController(
        IServiceService service,
        FixNearDbContext db)
    {
        _service = service;
        _db = db;
    }

    // Danh sách dịch vụ hiện tại - giữ tương thích API cũ
    [HttpGet]
    public async Task<IActionResult> GetServicesAsync()
    {
        var result = await _service.GetServicesAsync();
        return Ok(result);
    }

    // Tìm kiếm và phân trang dịch vụ cho Mobile
    [HttpGet("search")]
    public async Task<IActionResult> SearchServices(
        [FromQuery] string? keyword,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 50)
            return BadRequest(new
            {
                message = "Page phải >= 1, PageSize từ 1 đến 50."
            });

        if (categoryId.HasValue && categoryId.Value <= 0)
            return BadRequest(new
            {
                message = "CategoryId không hợp lệ."
            });

        keyword = keyword?.Trim();

        if (keyword?.Length > 100)
            return BadRequest(new
            {
                message = "Từ khóa tối đa 100 ký tự."
            });

        var query = _db.Service
            .AsNoTracking()
            .Where(s => s.IsActive && s.Category.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(s =>
                s.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = "%" +
                keyword.Replace("[", "[[]")
                       .Replace("%", "[%]")
                       .Replace("_", "[_]") +
                "%";

            query = query.Where(s =>
                EF.Functions.Like(s.ServiceName, pattern) ||
                EF.Functions.Like(s.Category.CategoryName, pattern) ||
                (s.Description != null &&
                 EF.Functions.Like(s.Description, pattern)));
        }

        var total = await query.CountAsync();

        var data = await query
            .OrderBy(s => s.ServiceName)
            .ThenBy(s => s.ServiceId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ServiceResponseDto
            {
                ServiceId = s.ServiceId,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.CategoryName,
                ServiceName = s.ServiceName,
                Description = s.Description
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(
                total / (double)pageSize),
            data
        });
    }

    // Chi tiết dịch vụ hiện tại
    [HttpGet("{serviceId:int}")]
    public async Task<IActionResult> GetServiceByIdAsync(
        int serviceId)
    {
        try
        {
            var result = await _service
                .GetServiceByIdAsync(serviceId);

            return Ok(result);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
    }
}
