namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for SMB/CIFS file sharing services
/// </summary>
public class SmbParser : DeviceParserBase
{

    public SmbParser(ILogger<SmbParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_smb") || HasServiceType(device, "_cifs");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new SmbInfo { Available = false };

        try
        {
            // Get all SMB endpoints
            var smbEndpoints = GetEndpointsByServiceType(device, "_smb")
                .Concat(GetEndpointsByServiceType(device, "_cifs"))
                .ToList();

            if (!smbEndpoints.Any())
            {
                // Fallback to primary device
                smbEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_smb._tcp.local.", 445)
                };
            }

            var primaryEndpoint = smbEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse common SMB TXT record fields from primary endpoint using helpers
            info.ServerName = GetTxtProperty(info.Properties, "server");
            info.Description = GetTxtProperty(info.Properties, "description", "desc");
            info.Workgroup = GetTxtProperty(info.Properties, "workgroup", "domain");

            // Collect shares from ALL SMB endpoints (each endpoint may advertise different shares)
            foreach (var endpoint in smbEndpoints)
            {
                // Share information from "shares" field (comma-separated list)
                if (endpoint.Properties.ContainsKey("shares"))
                {
                    var shares = endpoint.Properties["shares"].Split(',').Select(s => s.Trim());
                    foreach (var share in shares)
                    {
                        if (!string.IsNullOrWhiteSpace(share) && !info.Shares.Contains(share))
                        {
                            info.Shares.Add(share);
                        }
                    }
                }

                // Share information from "share" field (single share)
                if (endpoint.Properties.ContainsKey("share"))
                {
                    var share = endpoint.Properties["share"].Trim();
                    if (!string.IsNullOrWhiteSpace(share) && !info.Shares.Contains(share))
                    {
                        info.Shares.Add(share);
                    }
                }
            }

            // Version information
            info.Version = GetTxtProperty(info.Properties, "version");
            info.SmbVersion = GetTxtProperty(info.Properties, "smbversion");

            // Authentication
            info.Authentication = GetTxtProperty(info.Properties, "auth");
            info.GuestAccessEnabled = GetTxtBooleanProperty(info.Properties, "guest");

            // Identify device type based on name and properties
            info.DeviceType = IdentifyDeviceType(device.Name, info);

            info.Available = true;
            Logger.LogInformation("SMB file server detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query SMB info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }

    private static string IdentifyDeviceType(string deviceName, SmbInfo info)
    {
        var name = deviceName.ToLowerInvariant();

        // NAS devices
        if (name.Contains("synology") || name.Contains("diskstation"))
            return "Synology NAS";
        if (name.Contains("qnap"))
            return "QNAP NAS";
        if (name.Contains("netgear"))
            return "NetGear NAS";
        if (name.Contains("buffalo"))
            return "Buffalo NAS";
        if (name.Contains("drobo"))
            return "Drobo NAS";
        if (name.Contains("terramaster"))
            return "TerraMaster NAS";
        if (name.Contains("asustor"))
            return "ASUSTOR NAS";

        // Operating systems
        if (name.Contains("windows") || info.Workgroup?.ToLowerInvariant().Contains("workgroup") == true)
            return "Windows PC";
        if (name.Contains("macos") || name.Contains("mac") || name.Contains("imac") || name.Contains("macbook"))
            return "macOS Computer";
        if (name.Contains("ubuntu") || name.Contains("debian") || name.Contains("fedora") || name.Contains("centos"))
            return "Linux Server";
        if (name.Contains("samba"))
            return "Samba Server";

        // Generic
        if (info.Shares.Any())
            return "File Server";

        return "SMB/CIFS Server";
    }
}

public class SmbInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? ServerName { get; set; }
    public string? Description { get; set; }
    public string? Workgroup { get; set; }
    public List<string> Shares { get; set; } = new();
    public string? Version { get; set; }
    public string? SmbVersion { get; set; }
    public string? Authentication { get; set; }
    public bool GuestAccessEnabled { get; set; }
    public string DeviceType { get; set; } = "SMB/CIFS Server";
}
