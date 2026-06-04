using System.Collections.Concurrent;
using System.CommandLine;
using mDNSDiscovery.Cli.Output;
using Spectre.Console;

namespace mDNSDiscovery.Cli.Commands;

/// <summary>Continuous discovery: keep scanning and refresh the view until Ctrl+C.</summary>
public static class WatchCommand
{
    private static readonly TimeSpan DeviceTtl = TimeSpan.FromMinutes(5);

    public static Command Create()
    {
        var serviceOption = CliOptions.Service();
        var timeoutOption = CliOptions.Timeout(defaultSeconds: 2);
        var intervalOption = CliOptions.Interval();
        var formatOption = CliOptions.Format();

        var command = new Command("watch", "Continuously discover devices until Ctrl+C.")
        {
            serviceOption,
            timeoutOption,
            intervalOption,
            formatOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var filters = parseResult.GetValue(serviceOption) ?? [];
            var timeout = parseResult.GetValue(timeoutOption);
            var interval = parseResult.GetValue(intervalOption);
            var format = parseResult.GetValue(formatOption);

            var serviceTypes = CliOptions.ResolveServiceTypes(filters);
            var scanner = new MdnsScanner();
            var scanTime = TimeSpan.FromSeconds(timeout);
            var delay = TimeSpan.FromSeconds(interval);
            var cache = new ConcurrentDictionary<string, DeviceInfo>();

            try
            {
                if (format == OutputFormat.Table)
                {
                    await WatchTableAsync(scanner, cache, serviceTypes, scanTime, delay, cancellationToken);
                }
                else
                {
                    await WatchStreamAsync(scanner, cache, serviceTypes, scanTime, delay, cancellationToken);
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                return 0; // Ctrl+C is the normal way to stop watching.
            }
        });

        return command;
    }

    private static async Task WatchTableAsync(
        MdnsScanner scanner,
        ConcurrentDictionary<string, DeviceInfo> cache,
        IReadOnlyList<string> serviceTypes,
        TimeSpan scanTime,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        await AnsiConsole.Live(DeviceFormatter.BuildTable([]))
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await scanner.ScanIntoAsync(cache, serviceTypes, scanTime, cancellationToken);
                    MdnsScanner.EvictOlderThan(cache, DeviceTtl);

                    var devices = cache.Values.OrderBy(d => d.Name).ToList();
                    ctx.UpdateTarget(DeviceFormatter.BuildTable(devices));
                    ctx.Refresh();

                    await Task.Delay(delay, cancellationToken);
                }
            });
    }

    // Non-table formats stream NDJSON: one JSON object per device, re-emitted each cycle.
    private static async Task WatchStreamAsync(
        MdnsScanner scanner,
        ConcurrentDictionary<string, DeviceInfo> cache,
        IReadOnlyList<string> serviceTypes,
        TimeSpan scanTime,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await scanner.ScanIntoAsync(cache, serviceTypes, scanTime, cancellationToken);
            MdnsScanner.EvictOlderThan(cache, DeviceTtl);

            var devices = cache.Values.OrderBy(d => d.Name).ToList();
            DeviceFormatter.WriteNdjson(devices);

            await Task.Delay(delay, cancellationToken);
        }
    }
}
