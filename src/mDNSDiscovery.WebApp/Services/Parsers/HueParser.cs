namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for Philips Hue Bridge devices
/// </summary>
public class HueParser : DeviceParserBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HueParser(ILogger<HueParser> logger, IHttpClientFactory httpClientFactory) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_hue");
    }

    public override async Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new HueInfo { Available = false };

        try
        {
            // Get all Hue endpoints
            var hueEndpoints = GetEndpointsByServiceType(device, "_hue");

            if (!hueEndpoints.Any())
            {
                // Fallback to primary device
                hueEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_hue._tcp.local.", 80)
                };
            }

            var primaryEndpoint = hueEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse common Hue TXT record fields using helpers
            info.BridgeId = GetTxtProperty(info.Properties, "bridgeid");
            info.ModelId = GetTxtProperty(info.Properties, "modelid");
            info.BridgeName = GetTxtProperty(info.Properties, "name");

            // Try to query the Hue API for more information
            // Try multiple endpoints as different Hue versions expose different paths
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ShortTimeout");

                var apiEndpoints = new[] { "/api/0/config", "/api/config", "/api/nouser/config", "/description.xml" };

                foreach (var endpoint in apiEndpoints)
                {
                    try
                    {
                        var apiUrl = $"http://{device.IPAddress}{endpoint}";
                        var response = await httpClient.GetAsync(apiUrl, cancellationToken).ConfigureAwait(false);

                        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                            // Try to pretty-print JSON
                            try
                            {
                                var jsonElement = System.Text.Json.JsonDocument.Parse(content);
                                info.ApiResponse = System.Text.Json.JsonSerializer.Serialize(jsonElement, new System.Text.Json.JsonSerializerOptions
                                {
                                    WriteIndented = true
                                });
                            }
                            catch
                            {
                                // If not JSON, store as-is
                                info.ApiResponse = content;
                            }

                            // Parse basic info from API response
                            if (content.Contains("\"apiversion\""))
                            {
                                var apiVersionMatch = System.Text.RegularExpressions.Regex.Match(content, "\"apiversion\"\\s*:\\s*\"([^\"]+)\"");
                                if (apiVersionMatch.Success)
                                    info.ApiVersion = apiVersionMatch.Groups[1].Value;
                            }

                            if (content.Contains("\"swversion\""))
                            {
                                var swVersionMatch = System.Text.RegularExpressions.Regex.Match(content, "\"swversion\"\\s*:\\s*\"([^\"]+)\"");
                                if (swVersionMatch.Success)
                                    info.SoftwareVersion = swVersionMatch.Groups[1].Value;
                            }

                            if (content.Contains("\"name\""))
                            {
                                var nameMatch = System.Text.RegularExpressions.Regex.Match(content, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                                if (nameMatch.Success && string.IsNullOrEmpty(info.BridgeName))
                                    info.BridgeName = nameMatch.Groups[1].Value;
                            }

                            Logger.LogInformation("Successfully queried Hue API at {Endpoint}", apiUrl);
                            break; // Stop trying other endpoints if one succeeds
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "Could not query Hue endpoint {Endpoint}", endpoint);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not query Hue API at {IP}", device.IPAddress);
            }

            info.Available = true;
            Logger.LogInformation("Philips Hue Bridge detected at {IP}:{Port}", device.IPAddress, info.Port);

            return info;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query Hue info for {DeviceName}", device.Name);

            // Still return the info object even if there was an error
            // This ensures the card appears with whatever data we could gather
            return new HueInfo
            {
                Available = true,
                Port = device.Port > 0 ? device.Port : 80,
                Properties = device.Properties
            };
        }
    }
}

public class HueInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? BridgeId { get; set; }
    public string? ModelId { get; set; }
    public string? BridgeName { get; set; }
    public string? ApiVersion { get; set; }
    public string? SoftwareVersion { get; set; }
    public string? ApiResponse { get; set; }
}
