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

    // Set on the background scan loop, read from client-side timers/handlers on other threads,
    // so it needs volatile semantics rather than a plain bool to guarantee cross-thread visibility.
    private volatile bool _isScanning;

    public MdnsDiscoveryService(MdnsScanner scanner, ILogger<MdnsDiscoveryService> logger)
    {
        _scanner = scanner;
        _logger = logger;
    }

    /// <summary>
    /// Raised after each scan+eviction cycle completes, so subscribers can push fresh data
    /// to clients (e.g. a Blazor component calling <c>StateHasChanged</c>) instead of polling.
    /// </summary>
    public event Action? DevicesChanged;

    /// <summary>
    /// True while a scan is actively in flight.
    /// </summary>
    public bool IsScanning => _isScanning;

    public IEnumerable<DeviceInfo> GetDevices() => _devices.Values.OrderBy(d => d.Name);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _isScanning = true;
                await _scanner.ScanIntoAsync(_devices, ServiceCatalog.Default, ScanTime, cancellationToken: stoppingToken);

                MdnsScanner.EvictOlderThan(_devices, DeviceTtl);
                _isScanning = false;

                DevicesChanged?.Invoke();

                await Task.Delay(ScanInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
                break;
            }
            catch (Exception ex)
            {
                _isScanning = false;
                _logger.LogError(ex, "Error during mDNS discovery");
                await Task.Delay(ErrorBackoff, stoppingToken);
            }
        }
    }
}
