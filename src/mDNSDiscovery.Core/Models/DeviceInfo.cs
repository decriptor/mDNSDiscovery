namespace mDNSDiscovery.Core;

/// <summary>
/// A device discovered on the local network, aggregated across one or more mDNS service types.
/// </summary>
public class DeviceInfo
{
    public string Name { get; set; } = "";
    public string IPAddress { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public DateTime LastSeen { get; set; }
    public List<ServiceEndpoint> Endpoints { get; set; } = new();
}
