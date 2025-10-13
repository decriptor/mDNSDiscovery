namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for RAOP (Remote Audio Output Protocol) - AirPlay audio streaming
/// </summary>
public class RaopParser : DeviceParserBase
{

    public RaopParser(ILogger<RaopParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_raop");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new RaopInfo { Available = false };

        try
        {
            var raopEndpoints = GetEndpointsByServiceType(device, "_raop");

            if (!raopEndpoints.Any())
            {
                raopEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_raop._tcp.local.", 5000)
                };
            }

            var primaryEndpoint = raopEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse RAOP TXT record fields using helpers
            info.DeviceModel = GetTxtProperty(info.Properties, "am");
            info.AudioChannels = GetTxtProperty(info.Properties, "ch");
            info.SampleSize = GetTxtProperty(info.Properties, "ss");
            info.SampleRate = GetTxtProperty(info.Properties, "sr");
            info.TransportProtocol = GetTxtProperty(info.Properties, "tp");
            info.ServerVersion = GetTxtProperty(info.Properties, "vs");
            info.VendorVersion = GetTxtProperty(info.Properties, "vn");
            info.ModelName = GetTxtProperty(info.Properties, "md");

            info.PasswordRequired = GetTxtBooleanProperty(info.Properties, "pw");

            var compression = GetTxtProperty(info.Properties, "cn");
            if (compression != null)
            {
                // Note: Manual split needed to pass to DecodeCompressionTypes
                var rawCompressionTypes = compression.Split(',').Select(c => c.Trim()).ToList();
                info.CompressionTypes = rawCompressionTypes;
                info.DecodedCompressionTypes = DecodeCompressionTypes(rawCompressionTypes);
            }

            var encryption = GetTxtProperty(info.Properties, "et");
            if (encryption != null)
            {
                info.EncryptionTypes = encryption;
                info.EncryptionSupported = DecodeEncryptionTypes(encryption);
            }

            var features = GetTxtProperty(info.Properties, "sf");
            if (features != null)
            {
                info.Features = features;
                info.DecodedFeatures = DecodeFeatures(features);
            }

            var featureFlags = GetTxtProperty(info.Properties, "ft");
            if (featureFlags != null)
            {
                info.FeatureFlags = featureFlags;
                info.DecodedFeatureFlags = DecodeFeatureFlags(featureFlags);
            }

            // Identify device type
            info.DeviceType = IdentifyDevice(device.Name, info.DeviceModel, info.ModelName);

            info.Available = true;
            Logger.LogInformation("RAOP service detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query RAOP info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }

    private static string IdentifyDevice(string name, string? deviceModel, string? modelName)
    {
        var nameLower = name.ToLowerInvariant();
        var model = (deviceModel ?? modelName ?? "").ToLowerInvariant();

        if (nameLower.Contains("airport") || model.Contains("airport"))
            return "AirPort Express";
        if (nameLower.Contains("apple tv") || model.Contains("appletv"))
            return "Apple TV";
        if (nameLower.Contains("homepod") || model.Contains("homepod"))
            return "HomePod";
        if (nameLower.Contains("iphone") || model.Contains("iphone"))
            return "iPhone";
        if (nameLower.Contains("ipad") || model.Contains("ipad"))
            return "iPad";
        if (nameLower.Contains("mac") || model.Contains("mac"))
            return "Mac";
        if (nameLower.Contains("sonos") || model.Contains("sonos"))
            return "Sonos Speaker";

        return "AirPlay Audio Device";
    }

    private static List<string> DecodeCompressionTypes(List<string> compressionCodes)
    {
        var types = new List<string>();
        foreach (var code in compressionCodes)
        {
            switch (code)
            {
                case "0": types.Add("PCM"); break;
                case "1": types.Add("ALAC (Apple Lossless)"); break;
                case "2": types.Add("AAC"); break;
                case "3": types.Add("AAC-ELD"); break;
                case "4": types.Add("OPUS"); break;
                default: types.Add($"Unknown ({code})"); break;
            }
        }
        return types;
    }

    private static List<string> DecodeEncryptionTypes(string? et)
    {
        var types = new List<string>();
        if (string.IsNullOrEmpty(et)) return types;

        var etValues = et.Split(',');
        foreach (var val in etValues)
        {
            switch (val.Trim())
            {
                case "0": types.Add("None"); break;
                case "1": types.Add("RSA"); break;
                case "2": types.Add("FairPlay"); break;
                case "3": types.Add("MFiSAP"); break;
                case "4": types.Add("FairPlay SAP 2.5"); break;
                default: types.Add($"Unknown ({val})"); break;
            }
        }
        return types;
    }

    private static List<string> DecodeFeatures(string? sf)
    {
        var features = new List<string>();
        if (string.IsNullOrEmpty(sf)) return features;

        if (long.TryParse(sf, System.Globalization.NumberStyles.HexNumber, null, out var flags))
        {
            if ((flags & 0x01) != 0) features.Add("Video");
            if ((flags & 0x02) != 0) features.Add("Photo");
            if ((flags & 0x04) != 0) features.Add("Video Fair Play");
            if ((flags & 0x08) != 0) features.Add("Video Volume Control");
            if ((flags & 0x10) != 0) features.Add("Video HTTP Live Streaming");
            if ((flags & 0x20) != 0) features.Add("Slideshow");
            if ((flags & 0x40) != 0) features.Add("Screen");
            if ((flags & 0x80) != 0) features.Add("Screen Rotate");
            if ((flags & 0x100) != 0) features.Add("Audio");
            if ((flags & 0x200) != 0) features.Add("Audio Redundant");
            if ((flags & 0x400) != 0) features.Add("FPS AAC ELD");
        }

        return features;
    }

    private static List<string> DecodeFeatureFlags(string? ft)
    {
        var features = new List<string>();
        if (string.IsNullOrEmpty(ft)) return features;

        // ft can be a single hex value or comma-separated hex values
        // Example: "0x4A7FDFD5,0xBC354BD0" or just "0x4A7FDFD5"
        var ftValues = ft.Split(',').Select(v => v.Trim()).ToList();

        foreach (var ftValue in ftValues)
        {
            var hexValue = ftValue.Replace("0x", "").Replace("0X", "");

            if (long.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out var flags))
            {
                // Decode feature flags based on AirPlay protocol specification
                // These are cumulative across all hex values if multiple are present

                // Audio codecs
                if ((flags & 0x01) != 0) features.Add("PCM");
                if ((flags & 0x02) != 0) features.Add("ALAC 16-bit 44.1kHz");
                if ((flags & 0x04) != 0) features.Add("ALAC 16-bit 48kHz");
                if ((flags & 0x08) != 0) features.Add("AAC-LC");
                if ((flags & 0x10) != 0) features.Add("AAC-ELD");

                // Audio features
                if ((flags & 0x20) != 0) features.Add("Audio Unencrypted");
                if ((flags & 0x40) != 0) features.Add("Audio Legacy Pairing");
                if ((flags & 0x80) != 0) features.Add("Audio MFi Pairing");
                if ((flags & 0x100) != 0) features.Add("Audio Unified Advertiser Info");
                if ((flags & 0x200) != 0) features.Add("Audio Buffered");
                if ((flags & 0x400) != 0) features.Add("Audio PTP Clock");
                if ((flags & 0x800) != 0) features.Add("Audio Screen");

                // Metadata support
                if ((flags & 0x4000) != 0) features.Add("Supports Metadata Text");
                if ((flags & 0x8000) != 0) features.Add("Supports Metadata Artwork");
                if ((flags & 0x10000) != 0) features.Add("Supports Metadata Progress");

                // Other capabilities
                if ((flags & 0x20000) != 0) features.Add("PIN Required");
                if ((flags & 0x40000) != 0) features.Add("Supports Relay");
                if ((flags & 0x80000) != 0) features.Add("Supports HK Access Control");
                if ((flags & 0x100000) != 0) features.Add("Supports FairPlay v3");
                if ((flags & 0x200000) != 0) features.Add("Supports Volume Control");
                if ((flags & 0x400000) != 0) features.Add("Supports Transient Pairing");
                if ((flags & 0x800000) != 0) features.Add("Supports Remote Control");

                // High-quality audio
                if ((flags & 0x8000000) != 0) features.Add("ALAC 24-bit 44.1kHz");
                if ((flags & 0x10000000) != 0) features.Add("ALAC 24-bit 48kHz");
                if ((flags & 0x20000000) != 0) features.Add("ALAC 24-bit 96kHz");
                if ((flags & 0x40000000) != 0) features.Add("ALAC 24-bit 192kHz");
            }
        }

        return features.Distinct().ToList();
    }
}

public class RaopInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? DeviceModel { get; set; }
    public string? ModelName { get; set; }
    public string DeviceType { get; set; } = "AirPlay Audio Device";
    public List<string> CompressionTypes { get; set; } = new();
    public List<string> DecodedCompressionTypes { get; set; } = new();
    public string? AudioChannels { get; set; }
    public string? SampleSize { get; set; }
    public string? SampleRate { get; set; }
    public string? TransportProtocol { get; set; }
    public string? ServerVersion { get; set; }
    public string? VendorVersion { get; set; }
    public bool PasswordRequired { get; set; }
    public string? EncryptionTypes { get; set; }
    public List<string> EncryptionSupported { get; set; } = new();
    public string? Features { get; set; }
    public List<string> DecodedFeatures { get; set; } = new();
    public string? FeatureFlags { get; set; }
    public List<string> DecodedFeatureFlags { get; set; } = new();
}
