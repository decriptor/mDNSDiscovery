using System.Text;
using Claunia.PropertyList;

namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for Apple AirPlay and RAOP devices
/// </summary>
public class AirPlayParser : DeviceParserBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AirPlayParser(IHttpClientFactory httpClientFactory, ILogger<AirPlayParser> logger) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override bool CanParse(DeviceInfo device)
    {
        var vendor = DeviceIconHelper.GetDeviceVendor(device);

        return vendor == "Apple" ||
               HasServiceType(device, "_airplay") ||
               HasServiceType(device, "_raop");
    }

    public override async Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new AirPlayInfo { Available = false, Endpoints = new List<AirPlayEndpoint>() };

        try
        {
            var httpClient = CreateHttpClientWithTimeout(_httpClientFactory, "InsecureClient", 2);

            // Get all AirPlay/RAOP endpoints
            var airplayEndpoints = GetEndpointsByServiceType(device, "_airplay")
                .Concat(GetEndpointsByServiceType(device, "_raop"))
                .ToList();

            if (!airplayEndpoints.Any())
            {
                // Fallback to primary port
                airplayEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_airplay._tcp.local.", 7000)
                };
            }

            // Query each endpoint
            foreach (var endpoint in airplayEndpoints)
            {
                try
                {
                    var url = $"http://{device.IPAddress}:{endpoint.Port}/info";
                    Logger.LogInformation("Querying AirPlay info from {Url}", url);

                    var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsByteArrayAsync();
                        var airplayEndpoint = new AirPlayEndpoint
                        {
                            Port = endpoint.Port,
                            ServiceType = endpoint.ServiceType,
                            Available = true
                        };

                        // Try to parse as binary plist
                        try
                        {
                            if (content.Length > 0 && content[0] == 'b' && content.Length > 8)
                            {
                                var plist = (NSDictionary)PropertyListParser.Parse(content);
                                airplayEndpoint.ParsedData = ParseAirPlayPlist(plist);
                                airplayEndpoint.RawData = Encoding.UTF8.GetString(content);
                            }
                            else
                            {
                                airplayEndpoint.RawData = Encoding.UTF8.GetString(content);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug(ex, "Failed to parse plist for AirPlay endpoint at port {Port}", endpoint.Port);
                            airplayEndpoint.RawData = Encoding.UTF8.GetString(content);
                        }

                        info.Endpoints.Add(airplayEndpoint);
                        info.Available = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Failed to query AirPlay endpoint at port {Port}", endpoint.Port);
                }
            }

            return info.Available ? info : null;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query AirPlay info for {DeviceName}", device.Name);
        }

        return null;
    }

    private Dictionary<string, object> ParseAirPlayPlist(NSDictionary plist)
    {
        var result = new Dictionary<string, object>();

        foreach (var key in plist.Keys)
        {
            var keyStr = key.ToString();
            var value = plist[key];

            if (value is NSString nsString)
            {
                result[keyStr] = nsString.ToString();
            }
            else if (value is NSNumber nsNumber)
            {
                result[keyStr] = nsNumber.ToString();
            }
            else if (value is NSArray nsArray)
            {
                var list = new List<object>();
                for (int i = 0; i < nsArray.Count; i++)
                {
#pragma warning disable CS0612 // Type or member is obsolete
                    var item = nsArray.ObjectAtIndex(i);
#pragma warning restore CS0612 // Type or member is obsolete
                    if (item is NSDictionary dict)
                    {
                        list.Add(ParseAirPlayPlist(dict));
                    }
                    else if (item != null)
                    {
                        list.Add(item.ToString() ?? "");
                    }
                }
                result[keyStr] = list;
            }
            else if (value is NSDictionary dict)
            {
                result[keyStr] = ParseAirPlayPlist(dict);
            }
            else if (value is NSData nsData)
            {
                result[keyStr] = Convert.ToBase64String(nsData.Bytes);
            }
            else
            {
                result[keyStr] = value?.ToString() ?? "";
            }
        }

        return result;
    }
}

public class AirPlayInfo
{
    public string? RawData { get; set; }
    public bool Available { get; set; }
    public List<AirPlayEndpoint> Endpoints { get; set; } = new();
}

public class AirPlayEndpoint
{
    public int Port { get; set; }
    public string ServiceType { get; set; } = "";
    public string? RawData { get; set; }
    public Dictionary<string, object>? ParsedData { get; set; }
    public bool Available { get; set; }
}
