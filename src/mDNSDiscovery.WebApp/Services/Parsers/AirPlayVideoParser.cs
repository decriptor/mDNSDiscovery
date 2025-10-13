namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for AirPlay video streaming (_airplay._tcp) - different from RAOP which is audio-only
/// </summary>
public class AirPlayVideoParser : DeviceParserBase
{

    public AirPlayVideoParser(ILogger<AirPlayVideoParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        // Look for _airplay but NOT _raop (which is handled by RaopParser)
        return HasServiceType(device, "_airplay") && !HasServiceType(device, "_raop");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new AirPlayVideoInfo { Available = false };

        try
        {
            var airplayEndpoints = GetEndpointsByServiceType(device, "_airplay")
                .Where(e => !e.ServiceType.Contains("_raop", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!airplayEndpoints.Any())
            {
                airplayEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_airplay._tcp.local.", 7000)
                };
            }

            var primaryEndpoint = airplayEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse AirPlay TXT record fields using helpers
            info.DeviceId = GetTxtProperty(info.Properties, "deviceid");

            var features = GetTxtProperty(info.Properties, "features");
            if (features != null)
            {
                info.Features = features;
                info.DecodedFeatures = DecodeFeatures(features);
            }

            info.Model = GetTxtProperty(info.Properties, "model");
            info.SourceVersion = GetTxtProperty(info.Properties, "srcvers");
            info.PairingIdentity = GetTxtProperty(info.Properties, "pi");
            info.PublicKey = GetTxtProperty(info.Properties, "pk");
            info.VolumeControl = GetTxtProperty(info.Properties, "vv");

            var flagsStr = GetTxtProperty(info.Properties, "flags");
            if (flagsStr != null && int.TryParse(flagsStr, System.Globalization.NumberStyles.HexNumber, null, out var flags))
            {
                info.SupportsAudio = (flags & 0x01) != 0;
                info.SupportsVideo = (flags & 0x02) != 0;
                info.RequiresPassword = (flags & 0x04) != 0;
                info.SupportsPhoto = (flags & 0x08) != 0;
            }

            info.PasswordRequired = GetTxtBooleanProperty(info.Properties, "pw");
            info.Width = GetTxtIntProperty(info.Properties, "width");
            info.Height = GetTxtIntProperty(info.Properties, "height");

            // Identify device type
            info.DeviceType = IdentifyDevice(device.Name, info.Model);

            info.Available = true;
            Logger.LogInformation("AirPlay video service detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query AirPlay video info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }

    private static string IdentifyDevice(string name, string? model)
    {
        var nameLower = name.ToLowerInvariant();
        var modelLower = (model ?? "").ToLowerInvariant();

        if (nameLower.Contains("apple tv") || modelLower.Contains("appletv"))
            return "Apple TV";
        if (nameLower.Contains("iphone") || modelLower.Contains("iphone"))
            return "iPhone";
        if (nameLower.Contains("ipad") || modelLower.Contains("ipad"))
            return "iPad";
        if (nameLower.Contains("mac") || modelLower.Contains("mac"))
            return "Mac";

        return "AirPlay Device";
    }

    private static List<string> DecodeFeatures(string? features)
    {
        var decoded = new List<string>();
        if (string.IsNullOrEmpty(features)) return decoded;

        // Features is a hex string representing capability flags
        var hexValue = features.Replace("0x", "").Replace("0X", "");
        if (long.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out var flags))
        {
            if ((flags & 0x01) != 0) decoded.Add("Video");
            if ((flags & 0x02) != 0) decoded.Add("Photo");
            if ((flags & 0x04) != 0) decoded.Add("Video FairPlay");
            if ((flags & 0x08) != 0) decoded.Add("Video Volume Control");
            if ((flags & 0x10) != 0) decoded.Add("Video HTTP Live Streaming");
            if ((flags & 0x20) != 0) decoded.Add("Slideshow");
            if ((flags & 0x40) != 0) decoded.Add("Screen Mirroring");
            if ((flags & 0x80) != 0) decoded.Add("Screen Rotation");
            if ((flags & 0x100) != 0) decoded.Add("Audio");
            if ((flags & 0x200) != 0) decoded.Add("Audio Redundant");
            if ((flags & 0x400) != 0) decoded.Add("FPS AAC ELD");
            if ((flags & 0x800) != 0) decoded.Add("Photo Caching");
            if ((flags & 0x4000) != 0) decoded.Add("Authentication 4");
            if ((flags & 0x8000) != 0) decoded.Add("Metadata");
            if ((flags & 0x20000) != 0) decoded.Add("Audio PCM");
            if ((flags & 0x40000) != 0) decoded.Add("Screen");
            if ((flags & 0x80000) != 0) decoded.Add("SourceVersion");
        }

        return decoded;
    }
}

public class AirPlayVideoInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? DeviceId { get; set; }
    public string? Features { get; set; }
    public List<string> DecodedFeatures { get; set; } = new();
    public string? Model { get; set; }
    public string DeviceType { get; set; } = "AirPlay Device";
    public string? SourceVersion { get; set; }
    public string? PairingIdentity { get; set; }
    public string? PublicKey { get; set; }
    public string? VolumeControl { get; set; }
    public bool SupportsAudio { get; set; }
    public bool SupportsVideo { get; set; }
    public bool RequiresPassword { get; set; }
    public bool SupportsPhoto { get; set; }
    public bool PasswordRequired { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}
