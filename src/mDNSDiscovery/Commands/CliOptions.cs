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

    public static Option<int> Timeout(int defaultSeconds)
    {
        var option = new Option<int>("--timeout", "-t")
        {
            Description = "Seconds to listen for responses in each scan window.",
            DefaultValueFactory = _ => defaultSeconds,
        };
        option.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int>() <= 0)
            {
                result.AddError("--timeout must be greater than 0.");
            }
        });
        return option;
    }

    public static Option<OutputFormat> Format()
    {
        return new Option<OutputFormat>("--format", "-f")
        {
            Description = "Output format: table, json, or ndjson.",
            HelpName = "table|json|ndjson",
            DefaultValueFactory = _ => OutputFormat.Table,
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return OutputFormat.Table;
                }

                var raw = result.Tokens[0].Value;
                var parsed = ParseFormat(raw);
                if (parsed is null)
                {
                    result.AddError($"'{raw}' is not a valid format. Expected: table, json, or ndjson.");
                    return OutputFormat.Table;
                }

                return parsed.Value;
            },
        };
    }

    public static Option<int> Interval() => new("--interval", "-i")
    {
        Description = "Seconds to wait between scan cycles.",
        DefaultValueFactory = _ => 30,
    };

    public static Option<int> Retries()
    {
        var option = new Option<int>("--retries", "-r")
        {
            Description = "Times to re-send the query per scan window; raise it on lossy networks so " +
                          "slow-to-answer devices are less likely to be missed.",
            DefaultValueFactory = _ => MdnsScanner.DefaultRetries,
        };
        option.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int>() < 0)
            {
                result.AddError("--retries cannot be negative.");
            }
        });
        return option;
    }

    /// <summary>Parses an output-format name (case-insensitive); returns null if unrecognized.</summary>
    public static OutputFormat? ParseFormat(string value) => value.ToLowerInvariant() switch
    {
        "table" => OutputFormat.Table,
        "json" => OutputFormat.Json,
        "ndjson" => OutputFormat.Ndjson,
        _ => null,
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

    /// <summary>
    /// Returns the filters that look like shorthands (no dot) but match no known catalog service
    /// type — likely typos. Callers can warn instead of silently scanning a bogus type.
    /// </summary>
    public static IReadOnlyList<string> UnknownShorthands(string[] filters)
    {
        if (filters is null)
        {
            return [];
        }

        return filters
            .Where(f => !string.IsNullOrWhiteSpace(f) && !f.Contains('.'))
            .Where(f => !ServiceCatalog.Default.Any(t => ShortName(t).Equals(f, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    /// <summary>Suggests the closest known catalog shorthand (edit distance ≤ 2), or null if none is close.</summary>
    public static string? SuggestShorthand(string input)
    {
        string? best = null;
        var bestDistance = int.MaxValue;

        foreach (var shortName in ServiceCatalog.Default.Select(ShortName).Distinct())
        {
            var distance = LevenshteinDistance(input, shortName);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = shortName;
            }
        }

        return bestDistance <= 2 ? best : null;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            d[i, 0] = i;
        }
        for (var j = 0; j <= b.Length; j++)
        {
            d[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }

    private static string ShortName(string serviceType)
    {
        var trimmed = serviceType.TrimStart('_');
        var dot = trimmed.IndexOf('.');
        return dot >= 0 ? trimmed[..dot] : trimmed;
    }
}
