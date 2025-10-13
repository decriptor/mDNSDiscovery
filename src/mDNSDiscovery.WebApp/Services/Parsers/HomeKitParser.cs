using System.Text.Json;

namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for Apple HomeKit (HAP) devices
/// </summary>
public class HomeKitParser : DeviceParserBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeKitParser(IHttpClientFactory httpClientFactory, ILogger<HomeKitParser> logger) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_hap") || HasServiceType(device, "_homekit");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new HomeKitInfo { Available = false };

        try
        {
            // HomeKit uses TXT records for device information
            // No need for HTTP client as we extract info from mDNS records

            // Get all HAP/HomeKit endpoints
            var hapEndpoints = GetEndpointsByServiceType(device, "_hap")
                .Concat(GetEndpointsByServiceType(device, "_homekit"))
                .ToList();

            if (!hapEndpoints.Any())
            {
                // Fallback to primary port
                hapEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_hap._tcp.local.", 80)
                };
            }

            // Extract information from TXT records
            var primaryEndpoint = hapEndpoints.First();
            info.Properties = primaryEndpoint.Properties;

            // Parse common HAP TXT record fields using helpers
            info.ModelName = GetTxtProperty(info.Properties, "md");
            info.ProtocolVersion = GetTxtProperty(info.Properties, "pv");
            info.DeviceId = GetTxtProperty(info.Properties, "id");
            info.ConfigurationNumber = GetTxtProperty(info.Properties, "c#");
            info.StateNumber = GetTxtProperty(info.Properties, "s#");
            info.SetupHash = GetTxtProperty(info.Properties, "sh");

            var statusFlags = GetTxtProperty(info.Properties, "sf");
            if (statusFlags != null)
            {
                info.StatusFlags = statusFlags;
                info.StatusFlagsDecoded = DecodeStatusFlags(statusFlags);
            }

            var featureFlags = GetTxtProperty(info.Properties, "ff");
            if (featureFlags != null)
            {
                info.FeatureFlags = featureFlags;
                info.FeatureFlagsDecoded = DecodeFeatureFlags(featureFlags);
            }

            var categoryId = GetTxtProperty(info.Properties, "ci");
            if (categoryId != null)
            {
                info.CategoryIdentifier = categoryId;
                info.Category = GetCategoryName(categoryId);
            }

            info.Available = true;
            info.Port = primaryEndpoint.Port;

            // Note: HAP uses encrypted communication and requires pairing
            // We can't query the accessories endpoint without being paired
            Logger.LogInformation("HomeKit device detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query HomeKit info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }

    private static List<string> DecodeStatusFlags(string statusFlags)
    {
        var flags = new List<string>();

        if (int.TryParse(statusFlags, out var value))
        {
            if ((value & 0x01) != 0) flags.Add("Not Paired");
            if ((value & 0x02) != 0) flags.Add("Not Configured for WiFi");
            if ((value & 0x04) != 0) flags.Add("Problem Detected");
        }

        return flags;
    }

    private static List<string> DecodeFeatureFlags(string featureFlags)
    {
        var flags = new List<string>();

        if (int.TryParse(featureFlags, out var value))
        {
            if ((value & 0x01) != 0) flags.Add("Supports HAP Pairing");
            if ((value & 0x02) != 0) flags.Add("Supports Apple Authentication Coprocessor");
        }

        return flags;
    }

    private static string GetCategoryName(string categoryId)
    {
        return categoryId switch
        {
            "1" => "Other",
            "2" => "Bridge",
            "3" => "Fan",
            "4" => "Garage Door Opener",
            "5" => "Lightbulb",
            "6" => "Door Lock",
            "7" => "Outlet",
            "8" => "Switch",
            "9" => "Thermostat",
            "10" => "Sensor",
            "11" => "Security System",
            "12" => "Door",
            "13" => "Window",
            "14" => "Window Covering",
            "15" => "Programmable Switch",
            "16" => "Range Extender",
            "17" => "IP Camera",
            "18" => "Video Doorbell",
            "19" => "Air Purifier",
            "20" => "Heater",
            "21" => "Air Conditioner",
            "22" => "Humidifier",
            "23" => "Dehumidifier",
            "28" => "Sprinkler",
            "29" => "Faucet",
            "30" => "Shower System",
            "31" => "Television",
            "32" => "Target Remote Controller",
            "33" => "Router",
            "34" => "Audio Receiver",
            "35" => "TV Set-Top Box",
            "36" => "TV Streaming Stick",
            _ => $"Unknown ({categoryId})"
        };
    }
}

public class HomeKitInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? ModelName { get; set; }
    public string? ProtocolVersion { get; set; }
    public string? DeviceId { get; set; }
    public string? ConfigurationNumber { get; set; }
    public string? StateNumber { get; set; }
    public string? StatusFlags { get; set; }
    public List<string> StatusFlagsDecoded { get; set; } = new();
    public string? FeatureFlags { get; set; }
    public List<string> FeatureFlagsDecoded { get; set; } = new();
    public string? CategoryIdentifier { get; set; }
    public string? Category { get; set; }
    public string? SetupHash { get; set; }
}
