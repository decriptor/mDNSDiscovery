using System.Collections.Concurrent;
using mDNSDiscovery.Core;

namespace mDNSDiscovery.WebApp.Services;

/// <summary>
/// Background service that continuously discovers devices on the local network and keeps a
/// live cache of them. Scanning and model construction are delegated to <see cref="MdnsScanner"/>;
/// this service owns the persistent cache, the scan cadence, and stale-device eviction.
/// </summary>
public class MdnsDiscoveryService : BackgroundService
{
    private static readonly TimeSpan ScanTime = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DeviceTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, DeviceInfo> _devices = new();
    private readonly MdnsScanner _scanner;
    private readonly ILogger<MdnsDiscoveryService> _logger;

    public MdnsDiscoveryService(MdnsScanner scanner, ILogger<MdnsDiscoveryService> logger)
    {
        _scanner = scanner;
        _logger = logger;
    }

    public IEnumerable<DeviceInfo> GetDevices() => _devices.Values.OrderBy(d => d.Name);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _scanner.ScanIntoAsync(_devices, ServiceCatalog.Default, ScanTime, stoppingToken);

                MdnsScanner.EvictOlderThan(_devices, DeviceTtl);

                await Task.Delay(ScanInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during mDNS discovery");
                await Task.Delay(ErrorBackoff, stoppingToken);
            }
        }
    }
}
