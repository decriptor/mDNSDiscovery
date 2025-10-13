namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for IPP-compatible printer devices (_ipp._tcp, _printer._tcp)
/// </summary>
public class PrinterParser : DeviceParserBase
{

    public PrinterParser(ILogger<PrinterParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_printer") || HasServiceType(device, "_ipp");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        try
        {
            var info = new PrinterInfo { Available = true };

            // Find printer/IPP endpoints
            var printerEndpoints = GetEndpointsByServiceType(device, "_printer")
                .Concat(GetEndpointsByServiceType(device, "_ipp"))
                .ToList();

            if (!printerEndpoints.Any())
            {
                // Fallback to primary device
                printerEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, device.ServiceType, 631)
                };
            }

            var primaryEndpoint = printerEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Standard IPP endpoint
            info.IppEndpoint = $"http://{device.IPAddress}:{info.Port}/ipp/print";

            // Parse common printer TXT record fields using helpers
            info.DeviceType = GetTxtProperty(info.Properties, "ty");
            info.Product = GetTxtProperty(info.Properties, "product");
            info.Note = GetTxtProperty(info.Properties, "note");
            info.AdminUrl = GetTxtProperty(info.Properties, "adminurl");
            info.SupportedFormats = GetTxtListProperty(info.Properties, "pdl");

            info.SupportsColor = GetTxtBooleanProperty(info.Properties, "Color");
            info.SupportsDuplex = GetTxtBooleanProperty(info.Properties, "Duplex");
            info.SupportsScan = GetTxtBooleanProperty(info.Properties, "Scan");
            info.SupportsFax = GetTxtBooleanProperty(info.Properties, "Fax");

            info.ResourcePath = GetTxtProperty(info.Properties, "rp");
            info.QueueTotal = GetTxtIntProperty(info.Properties, "qtotal");
            info.Priority = GetTxtIntProperty(info.Properties, "priority");
            info.TxtVersion = GetTxtProperty(info.Properties, "txtvers");
            info.UUID = GetTxtProperty(info.Properties, "UUID");
            info.PrinterState = GetTxtProperty(info.Properties, "printer-state");

            info.UsbManufacturer = GetTxtProperty(info.Properties, "usb_MFG");
            info.UsbModel = GetTxtProperty(info.Properties, "usb_MDL");
            info.Manufacturer = GetTxtProperty(info.Properties, "mfg");
            info.Model = GetTxtProperty(info.Properties, "mdl");

            Logger.LogInformation("Printer detected at {IP}:{Port} - {Type}",
                device.IPAddress, info.Port, info.DeviceType ?? "Unknown");

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query printer info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }
}

public class PrinterInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? IppEndpoint { get; set; }
    public string? DeviceType { get; set; }
    public string? Product { get; set; }
    public string? Note { get; set; }
    public string? AdminUrl { get; set; }
    public List<string> SupportedFormats { get; set; } = new();
    public bool SupportsColor { get; set; }
    public bool SupportsDuplex { get; set; }
    public bool SupportsScan { get; set; }
    public bool SupportsFax { get; set; }
    public string? ResourcePath { get; set; }
    public int? QueueTotal { get; set; }
    public int? Priority { get; set; }
    public string? TxtVersion { get; set; }
    public string? UUID { get; set; }
    public string? PrinterState { get; set; }
    public string? UsbManufacturer { get; set; }
    public string? UsbModel { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
}
