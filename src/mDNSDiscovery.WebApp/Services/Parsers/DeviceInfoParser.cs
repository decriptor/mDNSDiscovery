namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for Device Info service (_device-info._tcp.local.)
/// Provides general device information
/// </summary>
public class DeviceInfoParser : DeviceParserBase
{

    public DeviceInfoParser(ILogger<DeviceInfoParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_device-info");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new GeneralDeviceInfo { Available = false };

        try
        {
            // Get all device-info endpoints
            var deviceInfoEndpoints = GetEndpointsByServiceType(device, "_device-info");

            if (!deviceInfoEndpoints.Any())
            {
                deviceInfoEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_device-info._tcp.local.", device.Port)
                };
            }

            var primaryEndpoint = deviceInfoEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse device-info TXT record fields using helpers with fallback keys
            info.Model = GetTxtProperty(info.Properties, "model");
            info.Manufacturer = GetTxtProperty(info.Properties, "manufacturer", "mfg");
            info.SerialNumber = GetTxtProperty(info.Properties, "serialNumber", "sn");
            info.FirmwareVersion = GetTxtProperty(info.Properties, "firmwareVersion", "fw");
            info.HardwareVersion = GetTxtProperty(info.Properties, "hardwareVersion", "hw");
            info.OSVersion = GetTxtProperty(info.Properties, "osVersion", "os");
            info.DeviceId = GetTxtProperty(info.Properties, "deviceId", "id");
            info.DeviceType = GetTxtProperty(info.Properties, "deviceType", "type");
            info.Description = GetTxtProperty(info.Properties, "description", "desc");
            info.Features = GetTxtListProperty(info.Properties, "features");

            info.Available = true;
            Logger.LogInformation("Device Info service detected at {IP}", device.IPAddress);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query Device Info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }
}

public class GeneralDeviceInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? HardwareVersion { get; set; }
    public string? OSVersion { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceType { get; set; }
    public List<string> Features { get; set; } = new();
    public string? Description { get; set; }
}
