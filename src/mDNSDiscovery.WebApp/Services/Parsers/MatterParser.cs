namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for Matter smart home devices
/// </summary>
public class MatterParser : DeviceParserBase
{

    public MatterParser(ILogger<MatterParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_matter") || HasServiceType(device, "_matterc");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new MatterInfo { Available = false };

        try
        {
            // Get all Matter endpoints
            var matterEndpoints = GetEndpointsByServiceType(device, "_matter");

            if (!matterEndpoints.Any())
            {
                matterEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_matter._tcp.local.", 5540)
                };
            }

            var primaryEndpoint = matterEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse Matter TXT record fields using helpers
            info.Discriminator = GetTxtProperty(info.Properties, "D");

            var vendorProduct = GetTxtProperty(info.Properties, "VP");
            if (vendorProduct != null)
            {
                info.VendorProduct = vendorProduct;
                // VP format is "VendorID+ProductID" in hex
                if (vendorProduct.Contains("+"))
                {
                    var parts = vendorProduct.Split('+');
                    if (parts.Length == 2)
                    {
                        info.VendorId = parts[0];
                        info.ProductId = parts[1];
                        info.VendorName = GetVendorName(info.VendorId);
                    }
                }
            }

            var deviceType = GetTxtProperty(info.Properties, "DT");
            if (deviceType != null)
            {
                info.DeviceType = deviceType;
                info.DeviceTypeName = GetDeviceTypeName(deviceType);
            }

            info.DeviceName = GetTxtProperty(info.Properties, "DN");
            info.SessionIdleInterval = GetTxtProperty(info.Properties, "SII");
            info.SessionActiveInterval = GetTxtProperty(info.Properties, "SAI");

            var transport = GetTxtProperty(info.Properties, "T");
            if (transport != null)
            {
                info.TransportProtocol = transport;
                info.SupportedTransports = DecodeTransportProtocol(transport);
            }

            var commMode = GetTxtProperty(info.Properties, "CM");
            if (commMode != null)
            {
                info.CommissioningMode = commMode;
                info.CommissioningModeDescription = DecodeCommissioningMode(commMode);
            }

            info.PairingHint = GetTxtProperty(info.Properties, "PH");
            info.PairingInstruction = GetTxtProperty(info.Properties, "PI");

            info.Available = true;
            Logger.LogInformation("Matter device detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query Matter info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }

    private static string GetVendorName(string vendorId)
    {
        // Common Matter vendor IDs (in hex)
        return vendorId?.ToUpper() switch
        {
            "6006" => "Apple",
            "E002" => "Google",
            "1321" => "Samsung",
            "100B" => "Amazon",
            "100D" => "IKEA",
            "117B" => "Philips/Signify",
            "1049" => "Nanoleaf",
            "1218" => "TP-Link",
            _ => $"Vendor {vendorId}"
        };
    }

    private static string GetDeviceTypeName(string? deviceType)
    {
        if (string.IsNullOrEmpty(deviceType))
            return "Unknown";

        // Convert hex to decimal for common device types
        if (int.TryParse(deviceType, System.Globalization.NumberStyles.HexNumber, null, out var dt))
        {
            return dt switch
            {
                0x000A => "Door Lock",
                0x000F => "On/Off Light",
                0x0010 => "Dimmable Light",
                0x0011 => "Color Temperature Light",
                0x0012 => "Extended Color Light",
                0x0100 => "On/Off Plug-in Unit",
                0x0101 => "Dimmable Plug-in Unit",
                0x0103 => "On/Off Light Switch",
                0x0104 => "Dimmer Switch",
                0x0105 => "Color Dimmer Switch",
                0x0106 => "Light Sensor",
                0x0107 => "Occupancy Sensor",
                0x0301 => "Thermostat",
                0x0302 => "Temperature Sensor",
                0x0305 => "Contact Sensor",
                0x0850 => "Window Covering",
                _ => $"Device Type {deviceType}"
            };
        }

        return $"Device Type {deviceType}";
    }

    private static List<string> DecodeTransportProtocol(string? transportValue)
    {
        var transports = new List<string>();

        if (string.IsNullOrEmpty(transportValue))
            return transports;

        try
        {
            if (int.TryParse(transportValue, out var value))
            {
                // T is a bitmap field according to Matter spec:
                // Bit 0 (0x01): UDP/BLE transport
                // Bit 1 (0x02): TCP transport
                // Bit 2 (0x04): Thread transport
                if ((value & 0x01) != 0) transports.Add("UDP/BLE");
                if ((value & 0x02) != 0) transports.Add("TCP");
                if ((value & 0x04) != 0) transports.Add("Thread");
            }
        }
        catch
        {
            // If parsing fails, return empty list
        }

        return transports;
    }

    private static string? DecodeCommissioningMode(string? commissioningMode)
    {
        if (string.IsNullOrEmpty(commissioningMode))
            return null;

        // CM field values according to Matter spec:
        // 0: Not commissioned, not accepting commissioning
        // 1: Standard commissioning mode - accepting commissioning
        // 2: Enhanced commissioning mode
        return commissioningMode switch
        {
            "0" => "Not Commissioned (Not Accepting)",
            "1" => "Standard Commissioning Mode",
            "2" => "Enhanced Commissioning Mode",
            _ => $"Unknown Mode ({commissioningMode})"
        };
    }
}

public class MatterInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? Discriminator { get; set; }
    public string? VendorProduct { get; set; }
    public string? VendorId { get; set; }
    public string? ProductId { get; set; }
    public string? VendorName { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceTypeName { get; set; }
    public string? DeviceName { get; set; }
    public string? SessionIdleInterval { get; set; }
    public string? SessionActiveInterval { get; set; }
    public string? TransportProtocol { get; set; }
    public List<string> SupportedTransports { get; set; } = new();
    public string? CommissioningMode { get; set; }
    public string? CommissioningModeDescription { get; set; }
    public string? PairingHint { get; set; }
    public string? PairingInstruction { get; set; }
}
