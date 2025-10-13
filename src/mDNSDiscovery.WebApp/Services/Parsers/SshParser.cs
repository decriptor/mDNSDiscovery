namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for SSH servers
/// </summary>
public class SshParser : DeviceParserBase
{

    public SshParser(ILogger<SshParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_ssh") || HasServiceType(device, "_sftp");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new SshInfo { Available = false };

        try
        {
            // Get all SSH/SFTP endpoints
            var sshEndpoints = GetEndpointsByServiceType(device, "_ssh")
                .Concat(GetEndpointsByServiceType(device, "_sftp"))
                .ToList();

            if (!sshEndpoints.Any())
            {
                sshEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_ssh._tcp.local.", 22)
                };
            }

            var primaryEndpoint = sshEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse SSH TXT record fields using helpers
            info.Username = GetTxtProperty(info.Properties, "u");
            info.DefaultPath = GetTxtProperty(info.Properties, "path");
            info.TxtVersion = GetTxtProperty(info.Properties, "txtvers");

            // Try to identify the device type based on name and properties
            var name = device.Name.ToLowerInvariant();
            if (name.Contains("raspberry") || name.Contains("rpi"))
                info.DeviceType = "Raspberry Pi";
            else if (name.Contains("synology") || name.Contains("diskstation"))
                info.DeviceType = "Synology NAS";
            else if (name.Contains("qnap"))
                info.DeviceType = "QNAP NAS";
            else if (name.Contains("ubuntu") || name.Contains("debian") || name.Contains("linux"))
                info.DeviceType = "Linux Server";
            else if (name.Contains("mac") || name.Contains("imac") || name.Contains("macbook"))
                info.DeviceType = "macOS Computer";
            else if (name.Contains("openwrt") || name.Contains("router"))
                info.DeviceType = "Router";
            else
                info.DeviceType = "SSH Server";

            info.Available = true;
            Logger.LogInformation("SSH server detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query SSH info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }
}

public class SshInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? Username { get; set; }
    public string? DefaultPath { get; set; }
    public string? TxtVersion { get; set; }
    public string? DeviceType { get; set; }
}
