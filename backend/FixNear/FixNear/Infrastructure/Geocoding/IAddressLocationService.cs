namespace FixNear.Infrastructure.Geocoding;

public interface IAddressLocationService
{
    Task<GeocodingResult?> SaveLocationAsync(int addressId);
}