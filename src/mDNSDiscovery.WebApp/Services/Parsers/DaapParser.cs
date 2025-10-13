namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for DAAP (Digital Audio Access Protocol) - iTunes/Music library sharing (_daap._tcp)
/// </summary>
public class DaapParser : DeviceParserBase
{

    public DaapParser(ILogger<DaapParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_daap");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new DaapInfo { Available = false };

        try
        {
            var daapEndpoints = GetEndpointsByServiceType(device, "_daap");

            if (!daapEndpoints.Any())
            {
                daapEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_daap._tcp.local.", 3689)
                };
            }

            var primaryEndpoint = daapEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse DAAP TXT record fields using helpers
            info.MachineName = GetTxtProperty(info.Properties, "Machine Name");
            info.TxtVersion = GetTxtProperty(info.Properties, "txtvers");
            info.iTunesShVersion = GetTxtProperty(info.Properties, "iTSh Version");
            info.Version = GetTxtProperty(info.Properties, "Version");
            info.DatabaseId = GetTxtProperty(info.Properties, "Database ID");
            info.MachineId = GetTxtProperty(info.Properties, "Machine ID");
            info.iTunesShareIndex = GetTxtProperty(info.Properties, "OSsi");

            info.PasswordProtected = GetTxtBooleanProperty(info.Properties, "Password");
            info.DatabasesCount = GetTxtIntProperty(info.Properties, "Databases Count");
            info.ItemsCount = GetTxtIntProperty(info.Properties, "Items Count");
            info.ContainersCount = GetTxtIntProperty(info.Properties, "Containers Count");

            var mediaKinds = GetTxtProperty(info.Properties, "Media Kinds Shared");
            if (mediaKinds != null)
            {
                info.MediaKindsShared = mediaKinds;
                info.DecodedMediaKinds = DecodeMediaKinds(mediaKinds);
            }

            info.Available = true;
            Logger.LogInformation("DAAP (iTunes/Music sharing) detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query DAAP info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }

    private static List<string> DecodeMediaKinds(string? mediaKinds)
    {
        var kinds = new List<string>();
        if (string.IsNullOrEmpty(mediaKinds)) return kinds;

        // Media kinds is a bitmap value
        if (int.TryParse(mediaKinds, out var value))
        {
            if ((value & 0x01) != 0) kinds.Add("Music");
            if ((value & 0x02) != 0) kinds.Add("Movies");
            if ((value & 0x04) != 0) kinds.Add("TV Shows");
            if ((value & 0x08) != 0) kinds.Add("Podcasts");
            if ((value & 0x10) != 0) kinds.Add("Audiobooks");
            if ((value & 0x20) != 0) kinds.Add("iTunes U");
            if ((value & 0x40) != 0) kinds.Add("Ringtones");
        }

        return kinds;
    }
}

public class DaapInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? MachineName { get; set; }
    public string? TxtVersion { get; set; }
    public string? iTunesShVersion { get; set; }
    public string? Version { get; set; }
    public string? DatabaseId { get; set; }
    public string? MachineId { get; set; }
    public bool PasswordProtected { get; set; }
    public string? MediaKindsShared { get; set; }
    public List<string> DecodedMediaKinds { get; set; } = new();
    public int? DatabasesCount { get; set; }
    public int? ItemsCount { get; set; }
    public int? ContainersCount { get; set; }
    public string? iTunesShareIndex { get; set; }
}
