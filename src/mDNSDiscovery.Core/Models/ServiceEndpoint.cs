namespace mDNSDiscovery.Core;

/// <summary>
/// A single advertised service endpoint (service type + port + TXT records) for a device.
/// </summary>
public class ServiceEndpoint
{
    public required string ServiceType { get; set; }
    public required int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public DateTime LastSeen { get; set; }
}
