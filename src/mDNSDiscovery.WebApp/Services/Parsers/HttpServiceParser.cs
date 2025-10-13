namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for HTTP service (_http._tcp.local / _https._tcp.local)
/// </summary>
public class HttpServiceParser : DeviceParserBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpServiceParser(ILogger<HttpServiceParser> logger, IHttpClientFactory httpClientFactory) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_http._tcp") || HasServiceType(device, "_https._tcp");
    }

    public override async Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new HttpServiceInfo { Available = false };

        try
        {
            // Get HTTP/HTTPS endpoints
            var httpEndpoints = GetEndpointsByServiceType(device, "_http")
                .Concat(GetEndpointsByServiceType(device, "_https"))
                .ToList();

            if (!httpEndpoints.Any())
            {
                // Fallback to primary device
                var isHttps = device.ServiceType.ToLowerInvariant().Contains("_https");
                httpEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, isHttps ? "_https._tcp.local." : "_http._tcp.local.", isHttps ? 443 : 80)
                };
            }

            var primaryEndpoint = httpEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;
            info.IsSecure = primaryEndpoint.ServiceType.ToLowerInvariant().Contains("_https");

            // Parse common HTTP service TXT record fields using helpers
            info.Path = GetTxtProperty(info.Properties, "path");
            info.Username = GetTxtProperty(info.Properties, "u");
            info.Password = GetTxtProperty(info.Properties, "p");
            info.Description = GetTxtProperty(info.Properties, "note", "description");
            info.Version = GetTxtProperty(info.Properties, "version", "ver");

            // Try to query the HTTP endpoint
            try
            {
                var httpClient = CreateHttpClientWithTimeout(_httpClientFactory, info.IsSecure ? "InsecureClient" : "", 3);

                var path = !string.IsNullOrEmpty(info.Path) ? info.Path : "/";
                var protocol = info.IsSecure ? "https" : "http";
                var url = $"{protocol}://{device.IPAddress}:{info.Port}{path}";

                info.Url = url;

                var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                info.ResponseStatusCode = (int)response.StatusCode;
                info.ResponseStatus = response.StatusCode.ToString();

                if (response.IsSuccessStatusCode)
                {
                    // Get headers
                    foreach (var header in response.Headers)
                    {
                        info.ResponseHeaders[header.Key] = string.Join(", ", header.Value);
                    }

                    // Try to detect server type
                    if (response.Headers.TryGetValues("Server", out var serverValues))
                    {
                        info.ServerType = string.Join(", ", serverValues);
                    }

                    // Read content (limit to 10KB to avoid large responses)
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(contentStream);
                    var buffer = new char[10240]; // 10KB
                    var charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                    info.ResponsePreview = new string(buffer, 0, charsRead);

                    // Try to detect content type
                    if (response.Content.Headers.ContentType != null)
                    {
                        info.ContentType = response.Content.Headers.ContentType.ToString();
                    }

                    Logger.LogInformation("Successfully queried HTTP service at {Url}", url);
                }
                else
                {
                    Logger.LogDebug("HTTP service at {Url} returned status {Status}", url, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not query HTTP service at {IP}:{Port}", device.IPAddress, info.Port);
            }

            info.Available = true;
            Logger.LogInformation("HTTP service detected at {IP}:{Port}", device.IPAddress, info.Port);

            return info;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query HTTP service for {DeviceName}", device.Name);

            // Still return info with basic data
            return new HttpServiceInfo
            {
                Available = true,
                Port = device.Port > 0 ? device.Port : 80,
                Properties = device.Properties
            };
        }
    }
}

public class HttpServiceInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public bool IsSecure { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? Path { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Url { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseStatus { get; set; }
    public string? ServerType { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();
    public string? ResponsePreview { get; set; }
}
