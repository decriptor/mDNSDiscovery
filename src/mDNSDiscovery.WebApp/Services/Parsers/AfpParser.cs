namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for AFP (Apple Filing Protocol) file sharing (_afpovertcp._tcp)
/// </summary>
public class AfpParser : DeviceParserBase
{

    public AfpParser(ILogger<AfpParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_afpovertcp");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new AfpInfo { Available = false };

        try
        {
            var afpEndpoints = GetEndpointsByServiceType(device, "_afpovertcp");

            if (!afpEndpoints.Any())
            {
                afpEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_afpovertcp._tcp.local.", 548)
                };
            }

            var primaryEndpoint = afpEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse AFP TXT record fields using helpers
            var sysCapabilities = GetTxtProperty(info.Properties, "sys");
            if (sysCapabilities != null)
            {
                info.SystemCapabilities = sysCapabilities;
                info.DecodedCapabilities = DecodeSystemCapabilities(sysCapabilities);
            }

            info.TxtVersion = GetTxtProperty(info.Properties, "txtvers");
            info.Model = GetTxtProperty(info.Properties, "model");
            info.MachineName = GetTxtProperty(info.Properties, "Machine Name");
            info.Version = GetTxtProperty(info.Properties, "Version");

            info.Available = true;
            Logger.LogInformation("AFP file sharing detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query AFP info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }

    private static List<string> DecodeSystemCapabilities(string? sys)
    {
        var caps = new List<string>();
        if (string.IsNullOrEmpty(sys)) return caps;

        // sys is typically a comma-separated list like "waMa=0,adVF=0x100,..."
        var parts = sys.Split(',');
        foreach (var part in parts)
        {
            var keyValue = part.Split('=');
            if (keyValue.Length == 2)
            {
                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();

                switch (key)
                {
                    case "waMa":
                        if (value != "0") caps.Add($"Wake for Network Access (MAC: {value})");
                        break;
                    case "adVF":
                        caps.Add("AFP Version Flags");
                        break;
                }
            }
        }

        return caps;
    }
}

public class AfpInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? SystemCapabilities { get; set; }
    public List<string> DecodedCapabilities { get; set; } = new();
    public string? TxtVersion { get; set; }
    public string? Model { get; set; }
    public string? MachineName { get; set; }
    public string? Version { get; set; }
}
