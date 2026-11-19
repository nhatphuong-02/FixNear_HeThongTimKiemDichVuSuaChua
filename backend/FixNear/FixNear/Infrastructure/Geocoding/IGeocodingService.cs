namespace FixNear.Infrastructure.Geocoding;

public interface IGeocodingService
{
    Task<GeocodingResult?> GetCoordinatesAsync(
        string address,
        CancellationToken cancellationToken = default);
}