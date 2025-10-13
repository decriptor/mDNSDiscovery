namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for ADisk (_adisk._tcp) - Apple Time Machine and Airport Disk
/// </summary>
public class ADiskParser : DeviceParserBase
{

    public ADiskParser(ILogger<ADiskParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_adisk");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new ADiskInfo { Available = false };

        try
        {
            var adiskEndpoints = GetEndpointsByServiceType(device, "_adisk");

            if (!adiskEndpoints.Any())
            {
                adiskEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_adisk._tcp.local.", 548)
                };
            }

            var primaryEndpoint = adiskEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse ADisk TXT record fields using helpers
            var sysFlags = GetTxtProperty(info.Properties, "sys");
            if (sysFlags != null)
            {
                info.SystemFlags = sysFlags;
                info.DecodedSystemFlags = DecodeSystemFlags(sysFlags);
            }

            var diskInfo = GetTxtProperty(info.Properties, "dk");
            if (diskInfo != null)
            {
                info.DiskInfo = diskInfo;
                ParseDiskInfo(diskInfo, info);
            }

            info.WakeMAAddress = GetTxtProperty(info.Properties, "waMa", "waMA");

            info.Available = true;
            Logger.LogInformation("ADisk service detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query ADisk info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }

    private static List<string> DecodeSystemFlags(string? sys)
    {
        var flags = new List<string>();
        if (string.IsNullOrEmpty(sys)) return flags;

        if (long.TryParse(sys, System.Globalization.NumberStyles.HexNumber, null, out var value))
        {
            if ((value & 0x01) != 0) flags.Add("Time Machine");
            if ((value & 0x02) != 0) flags.Add("AirPort Disk");
            if ((value & 0x04) != 0) flags.Add("Capsule");
            if ((value & 0x08) != 0) flags.Add("AFP");
            if ((value & 0x10) != 0) flags.Add("SMB");
        }

        return flags;
    }

    private static void ParseDiskInfo(string? dk, ADiskInfo info)
    {
        if (string.IsNullOrEmpty(dk)) return;

        // dk format: "adVF=0x<value>,adVN=<name>" or comma-separated list of disks
        var disks = dk.Split(',');
        foreach (var disk in disks)
        {
            var parts = disk.Trim().Split('=');
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (key == "adVN")
                {
                    info.VolumeName = value;
                }
                else if (key == "adVF")
                {
                    info.VolumeFlags = value;
                    if (value.StartsWith("0x") && long.TryParse(value.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var flags))
                    {
                        if ((flags & 0x01) != 0) info.DecodedVolumeFlags.Add("Time Machine Volume");
                        if ((flags & 0x02) != 0) info.DecodedVolumeFlags.Add("Network Volume");
                        if ((flags & 0x04) != 0) info.DecodedVolumeFlags.Add("Encrypted");
                    }
                }
            }
        }
    }
}

public class ADiskInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? SystemFlags { get; set; }
    public List<string> DecodedSystemFlags { get; set; } = new();
    public string? DiskInfo { get; set; }
    public string? VolumeName { get; set; }
    public string? VolumeFlags { get; set; }
    public List<string> DecodedVolumeFlags { get; set; } = new();
    public string? WakeMAAddress { get; set; }
}
