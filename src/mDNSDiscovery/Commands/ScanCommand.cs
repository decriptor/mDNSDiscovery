using System.CommandLine;
using mDNSDiscovery.Cli.Output;
using Spectre.Console;

namespace mDNSDiscovery.Cli.Commands;

/// <summary>One-shot discovery: scan for a fixed window, print results, exit.</summary>
public static class ScanCommand
{
    public static Command Create()
    {
        var serviceOption = CliOptions.Service();
        var timeoutOption = CliOptions.Timeout(defaultSeconds: 5);
        var formatOption = CliOptions.Format();

        var command = new Command("scan", "Discover devices once and print the results.")
        {
            serviceOption,
            timeoutOption,
            formatOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var filters = parseResult.GetValue(serviceOption) ?? [];
            var timeout = parseResult.GetValue(timeoutOption);
            var format = parseResult.GetValue(formatOption);

            var serviceTypes = CliOptions.ResolveServiceTypes(filters);
            var scanner = new MdnsScanner();
            var scanTime = TimeSpan.FromSeconds(timeout);

            try
            {
                IReadOnlyList<DeviceInfo> devices;
                if (format == OutputFormat.Table)
                {
                    devices = await AnsiConsole.Status()
                        .StartAsync(
                            $"Scanning {serviceTypes.Count} service type(s) for {timeout}s…",
                            _ => scanner.ScanAsync(serviceTypes, scanTime, cancellationToken));
                }
                else
                {
                    devices = await scanner.ScanAsync(serviceTypes, scanTime, cancellationToken);
                }

                DeviceFormatter.Write(devices, format);

                // Non-zero when nothing is found so scripts can branch on it.
                return devices.Count > 0 ? 0 : 2;
            }
            catch (OperationCanceledException)
            {
                return 130; // 128 + SIGINT
            }
        });

        return command;
    }
}
