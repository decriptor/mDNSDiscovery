using System.Collections.Concurrent;

namespace mDNSDiscovery.WebApp.Services;

public class DeviceIconService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DeviceIconService> _logger;
    private readonly ConcurrentDictionary<string, CachedIcon> _iconCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);

    public DeviceIconService(IHttpClientFactory httpClientFactory, ILogger<DeviceIconService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DeviceIconData?> GetDeviceIconAsync(DeviceInfo device)
    {
        // Check if device has an "ic" property
        if (!device.Properties.TryGetValue("ic", out var iconPath))
        {
            return null;
        }

        // Create cache key
        var cacheKey = $"{device.IPAddress}:{iconPath}";

        // Check cache
        if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
        {
            if (DateTime.UtcNow - cachedIcon.FetchedAt < _cacheExpiration)
            {
                return cachedIcon.Data;
            }
            // Remove expired entry
            _iconCache.TryRemove(cacheKey, out _);
        }

        // Fetch icon
        string? usedUrl = null;
        try
        {
            // Try HTTPS first (Chromecast/Google devices use HTTPS with self-signed certs)
            var httpsUrl = BuildIconUrl(device.IPAddress, device.Port, iconPath, useHttps: true);
            usedUrl = httpsUrl;
            _logger.LogDebug("Fetching device icon from {Url} for device {DeviceName}", httpsUrl, device.Name);

            var httpClient = _httpClientFactory.CreateClient("InsecureClient");
            httpClient.Timeout = TimeSpan.FromSeconds(1);

            var response = await httpClient.GetAsync(httpsUrl);
            _logger.LogDebug("Icon fetch response: {StatusCode} from {Url}", response.StatusCode, httpsUrl);

            // If HTTPS fails with empty response or error, try HTTP
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
            {
                var httpUrl = BuildIconUrl(device.IPAddress, device.Port, iconPath, useHttps: false);
                usedUrl = httpUrl;
                _logger.LogDebug("HTTPS failed, trying HTTP: {Url}", httpUrl);
                response = await httpClient.GetAsync(httpUrl);
                _logger.LogDebug("HTTP response: {StatusCode} from {Url}", response.StatusCode, httpUrl);
            }

            if (response.IsSuccessStatusCode)
            {
                var imageBytes = await response.Content.ReadAsByteArrayAsync();

                if (imageBytes.Length == 0)
                {
                    _logger.LogWarning("Empty icon response from {Url}", usedUrl);
                    _iconCache[cacheKey] = new CachedIcon { Data = null, FetchedAt = DateTime.UtcNow };
                    return null;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";

                var iconData = new DeviceIconData
                {
                    Data = imageBytes,
                    ContentType = contentType,
                    Base64 = Convert.ToBase64String(imageBytes)
                };

                _logger.LogInformation("Successfully fetched icon for {DeviceName} from {Url} ({Size} bytes)",
                    device.Name, usedUrl, imageBytes.Length);

                // Cache the result
                _iconCache[cacheKey] = new CachedIcon
                {
                    Data = iconData,
                    FetchedAt = DateTime.UtcNow
                };

                return iconData;
            }
            else
            {
                _logger.LogWarning("Failed to fetch icon from {Url}: {StatusCode}", usedUrl, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching icon from {IpAddress}:{Port}{Path} for device {DeviceName}",
                device.IPAddress, device.Port, iconPath, device.Name);

            // Cache the failure to avoid repeated attempts
            _iconCache[cacheKey] = new CachedIcon
            {
                Data = null,
                FetchedAt = DateTime.UtcNow
            };
            return null;
        }

        // Cache failure
        _iconCache[cacheKey] = new CachedIcon
        {
            Data = null,
            FetchedAt = DateTime.UtcNow
        };

        return null;
    }

    private static string BuildIconUrl(string ipAddress, int port, string iconPath, bool useHttps = true)
    {
        // Ensure iconPath starts with /
        if (!iconPath.StartsWith('/'))
        {
            iconPath = "/" + iconPath;
        }

        // Use the device's advertised port
        // If port is 0 or invalid, try common ports
        if (port <= 0 || port > 65535)
        {
            port = 8009; // Common Chromecast port
        }

        var protocol = useHttps ? "https" : "http";
        return $"{protocol}://{ipAddress}:{port}{iconPath}";
    }

    public void ClearCache()
    {
        _iconCache.Clear();
    }

    private class CachedIcon
    {
        public required DeviceIconData? Data { get; init; }
        public DateTime FetchedAt { get; init; }
    }
}

public class DeviceIconData
{
    public required byte[] Data { get; init; }
    public required string ContentType { get; init; }
    public required string Base64 { get; init; }

    public string GetDataUri() => $"data:{ContentType};base64,{Base64}";
}
