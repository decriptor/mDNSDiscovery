namespace mDNSDiscovery.WebApp.Services;

/// <summary>
/// Background service that periodically removes expired cache entries
/// </summary>
public class CacheCleanupService : BackgroundService
{
    private readonly DeviceCacheService _cacheService;
    private readonly ILogger<CacheCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public CacheCleanupService(DeviceCacheService cacheService, ILogger<CacheCleanupService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                _logger.LogDebug("Running cache cleanup");
                _cacheService.RemoveExpired();

                var stats = _cacheService.GetStatistics();
                _logger.LogDebug("Cache stats: {ActiveEntries} active, {ExpiredEntries} expired, {TotalEntries} total",
                    stats.ActiveEntries, stats.ExpiredEntries, stats.TotalEntries);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        _logger.LogInformation("Cache cleanup service stopped");
    }
}
