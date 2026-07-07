using Zeroconf;

namespace mDNSDiscovery.Tests.Fakes;

/// <summary>Minimal in-memory <see cref="IZeroconfHost"/> for driving MdnsScanner without a network.</summary>
public sealed class FakeZeroconfHost : IZeroconfHost
{
    public required string DisplayName { get; init; }
    public required string IPAddress { get; init; }
    public string Id { get; init; } = "";
    public IReadOnlyList<string> IPAddresses { get; init; } = [];
    public IReadOnlyDictionary<string, IService> Services { get; init; } = new Dictionary<string, IService>();

    /// <summary>Builds a host advertising the given (serviceType, port, txt) endpoints.</summary>
    public static FakeZeroconfHost Create(
        string displayName,
        string ip,
        params (string ServiceType, int Port, Dictionary<string, string>? Txt)[] endpoints)
    {
        var services = new Dictionary<string, IService>();
        foreach (var (serviceType, port, txt) in endpoints)
        {
            // Key on the instance name so it differs from the bare service type, mirroring Zeroconf.
            var instanceName = $"{displayName}.{serviceType}";
            services[instanceName] = new FakeService
            {
                Name = serviceType,
                ServiceName = instanceName,
                Port = port,
                Properties = [txt ?? new Dictionary<string, string>()],
            };
        }

        return new FakeZeroconfHost
        {
            DisplayName = displayName,
            IPAddress = ip,
            Id = ip,
            IPAddresses = [ip],
            Services = services,
        };
    }
}

public sealed class FakeService : IService
{
    public required string Name { get; init; }
    public string ServiceName { get; init; } = "";
    public required int Port { get; init; }
    public int Ttl { get; init; }
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Properties { get; init; } = [];
}
