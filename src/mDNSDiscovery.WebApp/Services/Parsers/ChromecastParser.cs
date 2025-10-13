using System.Text.Json;

namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for Google Chromecast devices
/// </summary>
public class ChromecastParser : DeviceParserBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ChromecastParser(IHttpClientFactory httpClientFactory, ILogger<ChromecastParser> logger) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override bool CanParse(DeviceInfo device)
    {
        var vendor = DeviceIconHelper.GetDeviceVendor(device);

        return vendor == "Google" || HasServiceType(device, "_googlecast");
    }

    public override async Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = CreateHttpClientWithTimeout(_httpClientFactory, "InsecureClient", 2);

            // Try /setup/eureka_info endpoint
            var url = $"http://{device.IPAddress}:8008/setup/eureka_info";
            Logger.LogInformation("Querying Chromecast info from {Url}", url);

            var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                return new ChromecastInfo
                {
                    RawData = json,
                    ParsedData = ParseJsonElement(data),
                    Name = data.GetProperty("name").GetString(),
                    BuildVersion = data.TryGetProperty("build_version", out var bv) ? bv.GetString() : null,
                    CastVersion = data.TryGetProperty("cast_build_revision", out var cv) ? cv.GetString() : null,
                    Model = data.TryGetProperty("model_name", out var mn) ? mn.GetString() : null,
                    Locale = data.TryGetProperty("locale", out var loc) ? loc.GetString() : null,
                    TimeZone = data.TryGetProperty("timezone", out var tz) ? tz.GetString() : null
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query Chromecast info for {DeviceName}", device.Name);
        }

        return null;
    }

    private Dictionary<string, object> ParseJsonElement(JsonElement element)
    {
        var result = new Dictionary<string, object>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ConvertJsonValue(property.Value);
            }
        }

        return result;
    }

    private object ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ParseJsonElement(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => "null",
            _ => element.ToString()
        };
    }
}

public class ChromecastInfo
{
    public string? RawData { get; set; }
    public Dictionary<string, object>? ParsedData { get; set; }
    public string? Name { get; set; }
    public string? BuildVersion { get; set; }
    public string? CastVersion { get; set; }
    public string? Model { get; set; }
    public string? Locale { get; set; }
    public string? TimeZone { get; set; }
}
