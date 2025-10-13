namespace mDNSDiscovery.WebApp.Services.Parsers;

public class HomebridgeParser : DeviceParserBase
{
    public HomebridgeParser(ILogger<HomebridgeParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device) => HasServiceType(device, "_homebridge");

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new HomebridgeInfo { Available = true, Port = device.Port, Properties = device.Properties };

        info.HomebridgeVersion = GetTxtProperty(info.Properties, "hbv");
        info.BridgeName = GetTxtProperty(info.Properties, "name");
        info.PluginCount = GetTxtProperty(info.Properties, "plugins");

        Logger.LogInformation("Homebridge detected at {IP}:{Port}", device.IPAddress, info.Port);
        return Task.FromResult<object?>(info);
    }
}

public class HomebridgeInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? HomebridgeVersion { get; set; }
    public string? BridgeName { get; set; }
    public string? PluginCount { get; set; }
}
