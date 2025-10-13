using System.Collections.Concurrent;

namespace mDNSDiscovery.WebApp.Services;

/// <summary>
/// In-memory cache for storing extended device query results
/// </summary>
public class DeviceCacheService
{
    private readonly ConcurrentDictionary<string, CachedDeviceInfo> _cache = new();
    private readonly ILogger<DeviceCacheService> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(10);

    public DeviceCacheService(ILogger<DeviceCacheService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get cached device extended info if available and not expired
    /// </summary>
    public DeviceExtendedInfo? Get(string deviceKey)
    {
        if (_cache.TryGetValue(deviceKey, out var cached))
        {
            if (cached.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Cache hit for device {DeviceKey}", deviceKey);
                return cached.ExtendedInfo;
            }
            else
            {
                // Remove expired entry
                _cache.TryRemove(deviceKey, out _);
                _logger.LogDebug("Cache expired for device {DeviceKey}", deviceKey);
            }
        }

        return null;
    }

    /// <summary>
    /// Store device extended info in cache
    /// </summary>
    public void Set(string deviceKey, DeviceExtendedInfo extendedInfo, TimeSpan? expiration = null)
    {
        var expiresAt = DateTime.UtcNow.Add(expiration ?? _defaultExpiration);

        _cache.AddOrUpdate(deviceKey,
            new CachedDeviceInfo
            {
                ExtendedInfo = extendedInfo,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            },
            (_, _) => new CachedDeviceInfo
            {
                ExtendedInfo = extendedInfo,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            });

        _logger.LogDebug("Cached device info for {DeviceKey}, expires at {ExpiresAt}", deviceKey, expiresAt);
    }

    /// <summary>
    /// Remove a specific device from cache
    /// </summary>
    public void Remove(string deviceKey)
    {
        if (_cache.TryRemove(deviceKey, out _))
        {
            _logger.LogDebug("Removed device {DeviceKey} from cache", deviceKey);
        }
    }

    /// <summary>
    /// Clear all cached entries
    /// </summary>
    public void Clear()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogInformation("Cleared {Count} cached device entries", count);
    }

    /// <summary>
    /// Remove expired entries from cache
    /// </summary>
    public void RemoveExpired()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Any())
        {
            _logger.LogDebug("Removed {Count} expired cache entries", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var now = DateTime.UtcNow;
        var entries = _cache.Values.ToList();

        return new CacheStatistics
        {
            TotalEntries = entries.Count,
            ExpiredEntries = entries.Count(e => e.ExpiresAt <= now),
            ActiveEntries = entries.Count(e => e.ExpiresAt > now),
            OldestEntry = entries.Any() ? entries.Min(e => e.CachedAt) : null,
            NewestEntry = entries.Any() ? entries.Max(e => e.CachedAt) : null
        };
    }
}

public class CachedDeviceInfo
{
    public required DeviceExtendedInfo ExtendedInfo { get; init; }
    public required DateTime CachedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

public class CacheStatistics
{
    public int TotalEntries { get; init; }
    public int ExpiredEntries { get; init; }
    public int ActiveEntries { get; init; }
    public DateTime? OldestEntry { get; init; }
    public DateTime? NewestEntry { get; init; }
}
