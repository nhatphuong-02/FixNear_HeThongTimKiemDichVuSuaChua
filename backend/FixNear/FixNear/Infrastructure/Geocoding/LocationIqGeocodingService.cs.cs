using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FixNear.Infrastructure.Geocoding;

public class LocationIqGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocationIqGeocodingService> _logger;

    public LocationIqGeocodingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LocationIqGeocodingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GeocodingResult?> GetCoordinatesAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        var apiKey = _configuration["LocationIQ:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("LocationIQ API key chưa được cấu hình.");
            return null;
        }

        var query = Uri.EscapeDataString(address.Trim());

        var url =
            $"https://us1.locationiq.com/v1/search" +
            $"?key={Uri.EscapeDataString(apiKey)}" +
            $"&q={query}" +
            $"&format=json" +
            $"&countrycodes=vn" +
            $"&accept-language=vi" +
            $"&limit=1";

        try
        {
            using var response = await _httpClient.GetAsync(
                url,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "LocationIQ trả về HTTP {StatusCode}.",
                    (int)response.StatusCode);

                return null;
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            var results = await JsonSerializer.DeserializeAsync<List<LocationIqResponse>>(
                stream,
                cancellationToken: cancellationToken);

            var firstResult = results?.FirstOrDefault();

            if (firstResult == null ||
                !decimal.TryParse(
                    firstResult.Latitude,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var latitude) ||
                !decimal.TryParse(
                    firstResult.Longitude,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var longitude))
            {
                return null;
            }

            return new GeocodingResult
            {
                Latitude = latitude,
                Longitude = longitude
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Không thể kết nối LocationIQ.");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Không thể đọc dữ liệu trả về từ LocationIQ.");
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Yêu cầu LocationIQ đã quá thời gian chờ.");
            return null;
        }
    }

    private class LocationIqResponse
    {
        [JsonPropertyName("lat")]
        public string? Latitude { get; set; }

        [JsonPropertyName("lon")]
        public string? Longitude { get; set; }
    }
}