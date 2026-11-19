using FixNear.Domain.Models.Locations;
using FixNear.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixNear.Infrastructure.Geocoding;

public class AddressLocationService : IAddressLocationService
{
    private readonly FixNearDbContext _context;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<AddressLocationService> _logger;

    public AddressLocationService(
        FixNearDbContext context,
        IGeocodingService geocodingService,
        ILogger<AddressLocationService> logger)
    {
        _context = context;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<GeocodingResult?> SaveLocationAsync(int addressId)
    {
        try
        {
            var address = await _context.Address
                .Where(a => a.AddressId == addressId)
                .Select(a => new
                {
                    a.AddressId,
                    a.AddressLine,
                    WardName = a.Ward.WardName,
                    ProvinceName = a.Province.ProvinceName
                })
                .FirstOrDefaultAsync();

            if (address == null)
                return null;

            // Nếu địa chỉ đã có tọa độ thì sử dụng lại
            var existingLocation = await _context.Location
                .FirstOrDefaultAsync(l => l.AddressId == addressId);

            if (existingLocation != null)
            {
                return new GeocodingResult
                {
                    Latitude = existingLocation.Latitude,
                    Longitude = existingLocation.Longitude
                };
            }

            var fullAddress =
                $"{address.AddressLine}, {address.WardName}, " +
                $"{address.ProvinceName}, Việt Nam";

            var coordinates =
                await _geocodingService.GetCoordinatesAsync(fullAddress);

            if (coordinates == null)
                return null;

            var location = new Location
            {
                AddressId = addressId,
                Latitude = coordinates.Latitude,
                Longitude = coordinates.Longitude,
                CreatedAt = DateTime.UtcNow
            };

            _context.Location.Add(location);
            await _context.SaveChangesAsync();

            return coordinates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Không thể lấy hoặc lưu tọa độ cho địa chỉ {AddressId}",
                addressId);

            // Tránh để entity lỗi vẫn ở trạng thái Added trong DbContext
            var entries = _context.ChangeTracker
                .Entries<Location>()
                .Where(e =>
                    e.State == EntityState.Added &&
                    e.Entity.AddressId == addressId)
                .ToList();

            foreach (var entry in entries)
                entry.State = EntityState.Detached;

            return null;
        }
    }
}