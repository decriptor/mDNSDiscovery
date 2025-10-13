using System.Text.Json;

namespace mDNSDiscovery.WebApp.Services;

/// <summary>
/// Specialized HTTP client for querying IoT devices with standardized patterns
/// ELIMINATES: Repeated HttpClient creation and configuration across parsers
/// </summary>
public class DeviceHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DeviceHttpClient> _logger;

    public DeviceHttpClient(IHttpClientFactory httpClientFactory, ILogger<DeviceHttpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Query an HTTP endpoint and return JSON response
    /// </summary>
    public async Task<T?> GetJsonAsync<T>(
        string url,
        string clientName = "ShortTimeout",
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient(clientName);
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to GET {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Query an HTTP endpoint and return raw string response
    /// </summary>
    public async Task<string?> GetStringAsync(
        string url,
        string clientName = "ShortTimeout",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient(clientName);
            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to GET {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Try multiple endpoints in sequence until one succeeds
    /// ELIMINATES: foreach (endpoint) pattern in HueParser and others
    /// </summary>
    public async Task<string?> TryMultipleEndpointsAsync(
        string ipAddress,
        string[] endpoints,
        string clientName = "ShortTimeout",
        CancellationToken cancellationToken = default)
    {
        foreach (var endpoint in endpoints)
        {
            var url = $"http://{ipAddress}{endpoint}";
            var result = await GetStringAsync(url, clientName, cancellationToken).ConfigureAwait(false);

            if (result != null)
            {
                _logger.LogDebug("Successfully queried {Url}", url);
                return result;
            }
        }

        return null;
    }
}
