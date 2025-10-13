namespace mDNSDiscovery.WebApp.Services.Parsers;

public class ScannerParser : DeviceParserBase
{
    public ScannerParser(ILogger<ScannerParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device) => HasServiceType(device, "_scanner");

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new ScannerInfo { Available = true, Port = device.Port, Properties = device.Properties };

        info.DeviceType = GetTxtProperty(info.Properties, "ty");
        info.AdminUrl = GetTxtProperty(info.Properties, "adminurl");
        info.Note = GetTxtProperty(info.Properties, "note");
        info.SupportedFormats = GetTxtListProperty(info.Properties, "pdl");
        info.DuplexSupported = GetTxtBooleanProperty(info.Properties, "duplex");

        Logger.LogInformation("Scanner detected at {IP}", device.IPAddress);
        return Task.FromResult<object?>(info);
    }
}

public class ScannerInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? DeviceType { get; set; }
    public string? AdminUrl { get; set; }
    public string? Note { get; set; }
    public List<string> SupportedFormats { get; set; } = new();
    public bool DuplexSupported { get; set; }
}
