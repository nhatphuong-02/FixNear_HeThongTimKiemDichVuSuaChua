using FixNear.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Features.Locations.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly FixNearDbContext _context;

    public LocationsController(FixNearDbContext context)
    {
        _context = context;
    }

    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces()
    {
        var provinces = await _context.Province
            .AsNoTracking()
            .OrderBy(p => p.ProvinceName)
            .Select(p => new
            {
                provinceId = p.ProvinceId,
                provinceName = p.ProvinceName
            })
            .ToListAsync();

        return Ok(provinces);
    }

    [HttpGet("wards")]
    public async Task<IActionResult> GetWards(
        [FromQuery] int provinceId)
    {
        if (provinceId <= 0)
            return BadRequest(new
            {
                message = "ProvinceId không hợp lệ"
            });

        var wards = await _context.Ward
            .AsNoTracking()
            .Where(w => w.ProvinceId == provinceId)
            .OrderBy(w => w.WardName)
            .Select(w => new
            {
                wardId = w.WardId,
                provinceId = w.ProvinceId,
                wardName = w.WardName
            })
            .ToListAsync();

        return Ok(wards);
    }
}