using System.CommandLine;

namespace mDNSDiscovery.Cli.Commands;

/// <summary>Factory methods for the options shared across commands, plus filter resolution.</summary>
public static class CliOptions
{
    public static Option<string[]> Service() => new("--service", "-s")
    {
        Description = "Service type to scan for; repeatable. Accepts shorthand (airplay) or a full " +
                      "type (_airplay._tcp.local.). Omit to scan the full catalog.",
        DefaultValueFactory = _ => [],
        AllowMultipleArgumentsPerToken = true,
    };

    public static Option<int> Timeout(int defaultSeconds) => new("--timeout", "-t")
    {
        Description = "Seconds to listen for responses in each scan window.",
        DefaultValueFactory = _ => defaultSeconds,
    };

    public static Option<OutputFormat> Format() => new("--format", "-f")
    {
        Description = "Output format: table, json, or ndjson.",
        DefaultValueFactory = _ => OutputFormat.Table,
    };

    public static Option<int> Interval() => new("--interval", "-i")
    {
        Description = "Seconds to wait between scan cycles.",
        DefaultValueFactory = _ => 30,
    };

    /// <summary>
    /// Resolves user-supplied service filters to concrete mDNS service types. Shorthand values
    /// (e.g. "airplay") are expanded to matching catalog entries; values already containing a dot
    /// are treated literally. An empty filter set means "scan the full catalog".
    /// </summary>
    public static IReadOnlyList<string> ResolveServiceTypes(string[] filters)
    {
        if (filters is null || filters.Length == 0)
        {
            return ServiceCatalog.Default;
        }

        var resolved = new List<string>();
        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                continue;
            }

            if (filter.Contains('.'))
            {
                resolved.Add(filter);
                continue;
            }

            var matches = ServiceCatalog.Default
                .Where(t => ShortName(t).Equals(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            resolved.AddRange(matches.Count > 0 ? matches : [$"_{filter}._tcp.local."]);
        }

        return resolved.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ShortName(string serviceType)
    {
        var trimmed = serviceType.TrimStart('_');
        var dot = trimmed.IndexOf('.');
        return dot >= 0 ? trimmed[..dot] : trimmed;
    }
}
