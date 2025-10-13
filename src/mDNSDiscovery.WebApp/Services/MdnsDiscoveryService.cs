using System.Collections.Concurrent;
using Zeroconf;

namespace mDNSDiscovery.WebApp.Services;

public class MdnsDiscoveryService : BackgroundService
{
    private readonly ConcurrentDictionary<string, DeviceInfo> _devices = new();
    private readonly ILogger<MdnsDiscoveryService> _logger;

    public MdnsDiscoveryService(ILogger<MdnsDiscoveryService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<DeviceInfo> GetDevices() => _devices.Values.OrderBy(d => d.Name);

    private DeviceInfo CreateDeviceInfo(IZeroconfHost response, string serviceType)
    {
        var device = new DeviceInfo
        {
            Name = response.DisplayName,
            IPAddress = response.IPAddress,
            ServiceType = serviceType,
            LastSeen = DateTime.UtcNow
        };

        // Extract all service endpoints
        if (response.Services.Any())
        {
            foreach (var service in response.Services)
            {
                var properties = service.Value.Properties
                    .SelectMany(p => p)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                device.Endpoints.Add(new ServiceEndpoint
                {
                    ServiceType = serviceType,
                    Port = service.Value.Port,
                    Properties = properties,
                    LastSeen = DateTime.UtcNow
                });

                // Set primary port and properties from first endpoint
                if (device.Port == 0)
                {
                    device.Port = service.Value.Port;
                    device.Properties = properties;
                }
            }
        }

        return device;
    }

    private DeviceInfo MergeDeviceInfo(DeviceInfo existing, IZeroconfHost response, string serviceType)
    {
        existing.LastSeen = DateTime.UtcNow;

        // Add or update endpoints from this service type
        if (response.Services.Any())
        {
            foreach (var service in response.Services)
            {
                var properties = service.Value.Properties
                    .SelectMany(p => p)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                // Check if endpoint already exists
                var existingEndpoint = existing.Endpoints
                    .FirstOrDefault(e => e.ServiceType == serviceType && e.Port == service.Value.Port);

                if (existingEndpoint != null)
                {
                    // Update existing endpoint
                    existingEndpoint.Properties = properties;
                    existingEndpoint.LastSeen = DateTime.UtcNow;
                }
                else
                {
                    // Add new endpoint
                    existing.Endpoints.Add(new ServiceEndpoint
                    {
                        ServiceType = serviceType,
                        Port = service.Value.Port,
                        Properties = properties,
                        LastSeen = DateTime.UtcNow
                    });
                }
            }
        }

        // Remove stale endpoints (not seen in last 5 minutes)
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        existing.Endpoints.RemoveAll(e => e.LastSeen < cutoff);

        return existing;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceTypes = new[]
        {
            "_http._tcp.local.",
            "_https._tcp.local.",
            "_printer._tcp.local.",
            "_ipp._tcp.local.",
            "_airplay._tcp.local.",
            "_googlecast._tcp.local.",
            "_homekit._tcp.local.",
            "_hap._tcp.local.",
            "_ssh._tcp.local.",
            "_sftp-ssh._tcp.local.",
            "_smb._tcp.local.",
            "_device-info._tcp.local.",
            "_raop._tcp.local.",
            "_sleep-proxy._udp.local.",
            "_sonos._tcp.local.",
            "_spotify-connect._tcp.local.",
            "_roku-rcp._tcp.local.",
            "_ecp._tcp.local.",  // Roku External Control Protocol
            "_hue._tcp.local.",
            "_companion-link._tcp.local.",  // Apple TV/HomePod
            "_airpointer._tcp.local.",  // AirPlay pointer device
            "_matter._tcp.local.",  // Matter smart home protocol
            "_matterc._udp.local."  // Matter commissioning
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var serviceType in serviceTypes)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    _logger.LogInformation("Scanning for {ServiceType}", serviceType);

                    var responses = await ZeroconfResolver.ResolveAsync(
                        serviceType,
                        scanTime: TimeSpan.FromSeconds(2),
                        cancellationToken: stoppingToken
                    );

                    foreach (var response in responses)
                    {
                        var key = $"{response.DisplayName}_{response.IPAddress}";

                        _devices.AddOrUpdate(key,
                            // Add factory - create new device
                            _ => CreateDeviceInfo(response, serviceType),
                            // Update factory - merge endpoints into existing device
                            (_, existingDevice) => MergeDeviceInfo(existingDevice, response, serviceType)
                        );
                    }
                }

                // Remove devices not seen in the last 5 minutes
                var cutoff = DateTime.UtcNow.AddMinutes(-5);
                var staleKeys = _devices.Where(kvp => kvp.Value.LastSeen < cutoff)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in staleKeys)
                {
                    _devices.TryRemove(key, out _);
                }

                // Wait before next scan cycle
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during mDNS discovery");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}

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

public class ServiceEndpoint
{
    public required string ServiceType { get; set; }
    public required int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public DateTime LastSeen { get; set; }
}
