namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for Apple Companion Link protocol
/// Used for communication between iOS/iPadOS devices and Macs
/// </summary>
public class CompanionLinkParser : DeviceParserBase
{

    public CompanionLinkParser(ILogger<CompanionLinkParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_companion-link");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new CompanionLinkInfo { Available = false };

        try
        {
            // Get all Companion Link endpoints
            var companionEndpoints = GetEndpointsByServiceType(device, "_companion-link");

            if (!companionEndpoints.Any())
            {
                companionEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_companion-link._tcp.local.", 49152)
                };
            }

            var primaryEndpoint = companionEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse Companion Link TXT record fields using helpers
            info.BluetoothAddress = GetTxtProperty(info.Properties, "rpBA");
            info.HomeKitAddress = GetTxtProperty(info.Properties, "rpHA");
            info.HostName = GetTxtProperty(info.Properties, "rpHN");
            info.AppleDeviceId = GetTxtProperty(info.Properties, "rpAD");
            info.ProtocolVersion = GetTxtProperty(info.Properties, "rpVr");
            info.ModelIdentifier = GetTxtProperty(info.Properties, "rpMd");
            info.MacAddress = GetTxtProperty(info.Properties, "rpMac");

            var featureFlags = GetTxtProperty(info.Properties, "rpFl");
            if (featureFlags != null)
            {
                info.FeatureFlags = featureFlags;
                info.DecodedFeatureFlags = DecodeFeatureFlags(featureFlags);
            }

            // Identify device type based on properties and name
            info.DeviceType = IdentifyAppleDevice(device.Name, info.ModelIdentifier);

            info.Available = true;
            Logger.LogInformation("Apple Companion Link detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query Companion Link info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }

    private static string IdentifyAppleDevice(string deviceName, string? modelIdentifier)
    {
        var name = deviceName.ToLowerInvariant();
        var model = modelIdentifier?.ToLowerInvariant() ?? "";

        // iPad models
        if (name.Contains("ipad") || model.Contains("ipad"))
        {
            if (model.Contains("ipad14,") || model.Contains("ipad13,"))
                return "iPad Pro (M2/M1)";
            if (model.Contains("ipad"))
                return "iPad";
            return "iPad";
        }

        // iPhone models
        if (name.Contains("iphone") || model.Contains("iphone"))
        {
            if (model.Contains("iphone16,") || model.Contains("iphone15,"))
                return "iPhone 15/16";
            if (model.Contains("iphone"))
                return "iPhone";
            return "iPhone";
        }

        // Mac models
        if (name.Contains("macbook") || model.Contains("macbook"))
            return "MacBook";
        if (name.Contains("imac") || model.Contains("imac"))
            return "iMac";
        if (name.Contains("mac mini") || model.Contains("macmini"))
            return "Mac mini";
        if (name.Contains("mac studio") || model.Contains("macstudio"))
            return "Mac Studio";
        if (name.Contains("mac pro") || model.Contains("macpro"))
            return "Mac Pro";

        // Apple Watch
        if (name.Contains("watch") || model.Contains("watch"))
            return "Apple Watch";

        // Apple TV
        if (name.Contains("apple tv") || model.Contains("appletv"))
            return "Apple TV";

        // HomePod
        if (name.Contains("homepod") || model.Contains("homepod"))
            return "HomePod";

        return "Apple Device";
    }

    private static List<string> DecodeFeatureFlags(string? featureFlagsHex)
    {
        var features = new List<string>();

        if (string.IsNullOrEmpty(featureFlagsHex))
            return features;

        try
        {
            long flags = 0;

            // Try parsing as decimal first (in case it's already a number)
            if (long.TryParse(featureFlagsHex, out flags))
            {
                // Successfully parsed as decimal
            }
            // Otherwise try as hex
            else
            {
                // Remove "0x" prefix if present
                var hexValue = featureFlagsHex.Replace("0x", "").Replace("0X", "");

                // Parse hex to long to support larger values
                if (!long.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out flags))
                {
                    return features; // Parsing failed
                }
            }

            // Based on Apple's Rapport/Companion Link protocol
            // These are common feature flags observed in the wild
            if ((flags & 0x01) != 0) features.Add("Handoff");
            if ((flags & 0x02) != 0) features.Add("HomeKit");
            if ((flags & 0x04) != 0) features.Add("AirPlay");
            if ((flags & 0x08) != 0) features.Add("AirDrop");
            if ((flags & 0x10) != 0) features.Add("Universal Clipboard");
            if ((flags & 0x20) != 0) features.Add("Phone Calls");
            if ((flags & 0x40) != 0) features.Add("Instant Hotspot");
            if ((flags & 0x80) != 0) features.Add("Unlock with Apple Watch");
            if ((flags & 0x100) != 0) features.Add("Sidecar");
            if ((flags & 0x200) != 0) features.Add("AirPlay to Mac");
            if ((flags & 0x400) != 0) features.Add("Camera Continuity");
            if ((flags & 0x800) != 0) features.Add("Keyboard Sharing");
            if ((flags & 0x1000) != 0) features.Add("Auto Unlock");
            if ((flags & 0x2000) != 0) features.Add("Find My");
            if ((flags & 0x4000) != 0) features.Add("Proximity Pairing");
            if ((flags & 0x8000) != 0) features.Add("Remote Management");
        }
        catch
        {
            // If parsing fails, return empty list
        }

        return features;
    }
}

public class CompanionLinkInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? BluetoothAddress { get; set; }
    public string? HomeKitAddress { get; set; }
    public string? HostName { get; set; }
    public string? AppleDeviceId { get; set; }
    public string? ProtocolVersion { get; set; }
    public string? FeatureFlags { get; set; }
    public List<string> DecodedFeatureFlags { get; set; } = new();
    public string? ModelIdentifier { get; set; }
    public string? MacAddress { get; set; }
    public string DeviceType { get; set; } = "Apple Device";
}
