using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Zeroconf;

namespace mDNSDiscovery.Core;

/// <summary>
/// Reusable mDNS / DNS-SD discovery engine. Resolves a set of service types via Zeroconf in a
/// single scan window and aggregates the responses into <see cref="DeviceInfo"/> records keyed by
/// name + IP. Stateless: callers own the cache, so the same engine drives both the web app's
/// long-running background scan and the CLI's one-shot and watch modes.
/// </summary>
public sealed class MdnsScanner
{
    /// <summary>Endpoints not seen within this window are dropped during a merge.</summary>
    private static readonly TimeSpan EndpointStaleAfter = TimeSpan.FromMinutes(5);

    private readonly ILogger<MdnsScanner> _logger;

    public MdnsScanner(ILogger<MdnsScanner>? logger = null)
    {
        _logger = logger ?? NullLogger<MdnsScanner>.Instance;
    }

    /// <summary>
    /// Performs a single discovery pass over <paramref name="serviceTypes"/> and returns a
    /// snapshot of the devices found, ordered by name.
    /// </summary>
    public async Task<IReadOnlyList<DeviceInfo>> ScanAsync(
        IEnumerable<string> serviceTypes,
        TimeSpan scanTime,
        CancellationToken cancellationToken = default)
    {
        var cache = new ConcurrentDictionary<string, DeviceInfo>();
        await ScanIntoAsync(cache, serviceTypes, scanTime, cancellationToken).ConfigureAwait(false);
        return cache.Values.OrderBy(d => d.Name).ToList();
    }

    /// <summary>
    /// Performs a single discovery pass and merges the results into <paramref name="cache"/>.
    /// All service types are resolved within one <paramref name="scanTime"/> window. Use this from
    /// a loop (web background service, CLI watch) to accumulate devices across passes; pair it with
    /// <see cref="EvictOlderThan"/> to drop devices that disappear.
    /// </summary>
    public async Task ScanIntoAsync(
        ConcurrentDictionary<string, DeviceInfo> cache,
        IEnumerable<string> serviceTypes,
        TimeSpan scanTime,
        CancellationToken cancellationToken = default)
    {
        var protocols = serviceTypes as IReadOnlyList<string> ?? serviceTypes.ToList();
        if (protocols.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Scanning {Count} service type(s) for {Seconds}s", protocols.Count, scanTime.TotalSeconds);

        var responses = await ZeroconfResolver.ResolveAsync(
            protocols,
            scanTime: scanTime,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        foreach (var response in responses)
        {
            var key = $"{response.DisplayName}_{response.IPAddress}";
            cache.AddOrUpdate(
                key,
                _ => CreateDeviceInfo(response),
                (_, existing) => MergeDeviceInfo(existing, response));
        }
    }

    /// <summary>Removes devices from <paramref name="cache"/> not seen within <paramref name="ttl"/>.</summary>
    public static void EvictOlderThan(ConcurrentDictionary<string, DeviceInfo> cache, TimeSpan ttl)
    {
        var cutoff = DateTime.UtcNow - ttl;
        var staleKeys = cache.Where(kvp => kvp.Value.LastSeen < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            cache.TryRemove(key, out _);
        }
    }

    private static DeviceInfo CreateDeviceInfo(IZeroconfHost response)
    {
        var now = DateTime.UtcNow;
        var device = new DeviceInfo
        {
            Name = response.DisplayName,
            IPAddress = response.IPAddress,
            LastSeen = now
        };

        foreach (var service in response.Services)
        {
            var serviceType = service.Value.Name;
            var properties = ExtractProperties(service.Value);

            device.Endpoints.Add(new ServiceEndpoint
            {
                ServiceType = serviceType,
                Port = service.Value.Port,
                Properties = properties,
                LastSeen = now
            });

            // Use the first advertised service as the device's primary endpoint.
            if (device.Port == 0)
            {
                device.ServiceType = serviceType;
                device.Port = service.Value.Port;
                device.Properties = properties;
            }
        }

        return device;
    }

    private static DeviceInfo MergeDeviceInfo(DeviceInfo existing, IZeroconfHost response)
    {
        var now = DateTime.UtcNow;
        existing.LastSeen = now;

        foreach (var service in response.Services)
        {
            var serviceType = service.Value.Name;
            var properties = ExtractProperties(service.Value);

            var existingEndpoint = existing.Endpoints
                .FirstOrDefault(e => e.ServiceType == serviceType && e.Port == service.Value.Port);

            if (existingEndpoint != null)
            {
                existingEndpoint.Properties = properties;
                existingEndpoint.LastSeen = now;
            }
            else
            {
                existing.Endpoints.Add(new ServiceEndpoint
                {
                    ServiceType = serviceType,
                    Port = service.Value.Port,
                    Properties = properties,
                    LastSeen = now
                });
            }

            if (existing.Port == 0)
            {
                existing.ServiceType = serviceType;
                existing.Port = service.Value.Port;
                existing.Properties = properties;
            }
        }

        // Drop endpoints that have gone stale.
        var cutoff = now - EndpointStaleAfter;
        existing.Endpoints.RemoveAll(e => e.LastSeen < cutoff);

        return existing;
    }

    private static Dictionary<string, string> ExtractProperties(IService service)
    {
        // IService.Properties is a list of TXT-record property sets; flatten, last value wins.
        var properties = new Dictionary<string, string>();
        foreach (var set in service.Properties)
        {
            foreach (var kv in set)
            {
                properties[kv.Key] = kv.Value;
            }
        }

        return properties;
    }
}
