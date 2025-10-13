namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for NFS (Network File System) shares (_nfs._tcp)
/// </summary>
public class NfsParser : DeviceParserBase
{

    public NfsParser(ILogger<NfsParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_nfs");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new NfsInfo { Available = false };

        try
        {
            var nfsEndpoints = GetEndpointsByServiceType(device, "_nfs");

            if (!nfsEndpoints.Any())
            {
                nfsEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_nfs._tcp.local.", 2049)
                };
            }

            var primaryEndpoint = nfsEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse NFS TXT record fields using helpers
            info.Path = GetTxtProperty(info.Properties, "path");
            info.TxtVersion = GetTxtProperty(info.Properties, "txtvers");

            var nfsVersion = GetTxtProperty(info.Properties, "vers");
            if (nfsVersion != null)
            {
                info.NfsVersion = nfsVersion;
                info.SupportedVersions = GetTxtListProperty(info.Properties, "vers");
            }

            var protocol = GetTxtProperty(info.Properties, "proto");
            if (protocol != null)
            {
                info.Protocol = protocol;
                // Note: Can't use GetTxtListProperty here because we need ToUpperInvariant()
                info.SupportedProtocols = protocol.Split(',').Select(p => p.Trim().ToUpperInvariant()).ToList();
            }

            info.Available = true;
            Logger.LogInformation("NFS share detected at {IP}:{Port} - Path: {Path}",
                device.IPAddress, info.Port, info.Path ?? "unknown");

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query NFS info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }
}

public class NfsInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? Path { get; set; }
    public string? NfsVersion { get; set; }
    public List<string> SupportedVersions { get; set; } = new();
    public string? Protocol { get; set; }
    public List<string> SupportedProtocols { get; set; } = new();
    public string? TxtVersion { get; set; }
}
