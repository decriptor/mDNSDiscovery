using System.Text.Json;
using Spectre.Console;

namespace mDNSDiscovery.Cli.Output;

/// <summary>Renders discovered devices to a table, a JSON array, or NDJSON.</summary>
public static class DeviceFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions NdjsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private const int NameColumnWidth = 32;
    private const int ServicesColumnWidth = 28;

    /// <summary>Builds a renderable table of devices (Name · IP · Services · Port).</summary>
    public static Table BuildTable(IReadOnlyList<DeviceInfo> devices)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("IP Address")
            .AddColumn("Services")
            .AddColumn(new TableColumn("Port").RightAligned());

        foreach (var device in devices)
        {
            var name = string.IsNullOrWhiteSpace(device.Name) ? "(unnamed)" : device.Name;
            table.AddRow(
                Markup.Escape(Truncate(name, NameColumnWidth)),
                Markup.Escape(device.IPAddress),
                Markup.Escape(Truncate(string.Join(", ", ShortServiceNames(device)), ServicesColumnWidth)),
                device.Port > 0 ? device.Port.ToString() : "-");
        }

        return table;
    }

    /// <summary>
    /// Truncates <paramref name="text"/> to at most <paramref name="maxLength"/> characters,
    /// appending an ellipsis so long device names stay on one table row.
    /// </summary>
    public static string Truncate(string text, int maxLength)
    {
        if (maxLength <= 0 || text.Length <= maxLength)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, maxLength - 1), "…");
    }

    /// <summary>Writes the devices in the requested format to the console (table/json/ndjson).</summary>
    public static void Write(IReadOnlyList<DeviceInfo> devices, OutputFormat format)
    {
        switch (format)
        {
            case OutputFormat.Json:
                Console.WriteLine(JsonSerializer.Serialize(devices, JsonOptions));
                break;

            case OutputFormat.Ndjson:
                WriteNdjson(devices);
                break;

            case OutputFormat.Table:
            default:
                if (devices.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No devices found.[/]");
                    return;
                }

                AnsiConsole.Write(BuildTable(devices));
                AnsiConsole.MarkupLine($"[grey]{devices.Count} device(s).[/]");
                break;
        }
    }

    /// <summary>
    /// Renders a list of service-type strings as a JSON array (<see cref="OutputFormat.Json"/>) or
    /// as NDJSON — one JSON string per line (<see cref="OutputFormat.Ndjson"/>). Used by list-types.
    /// </summary>
    public static string FormatServiceTypes(IReadOnlyList<string> serviceTypes, OutputFormat format) => format switch
    {
        OutputFormat.Ndjson => string.Join('\n', serviceTypes.Select(t => JsonSerializer.Serialize(t, NdjsonOptions))),
        OutputFormat.Json => JsonSerializer.Serialize(serviceTypes, JsonOptions),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Only Json and Ndjson are supported here."),
    };

    /// <summary>Writes one compact JSON object per device, each on its own line.</summary>
    public static void WriteNdjson(IReadOnlyList<DeviceInfo> devices)
    {
        foreach (var device in devices)
        {
            Console.WriteLine(JsonSerializer.Serialize(device, NdjsonOptions));
        }
    }

    /// <summary>
    /// Distinct, shortened service names for a device, e.g. "_airplay._tcp.local." -> "airplay".
    /// </summary>
    public static IEnumerable<string> ShortServiceNames(DeviceInfo device)
    {
        var serviceTypes = new List<string> { device.ServiceType };
        serviceTypes.AddRange(device.Endpoints.Select(e => e.ServiceType));

        return serviceTypes
            .Select(Shorten)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase);
    }

    private static string Shorten(string serviceType)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            return string.Empty;
        }

        var trimmed = serviceType.TrimStart('_');
        var dot = trimmed.IndexOf('.');
        return dot >= 0 ? trimmed[..dot] : trimmed;
    }
}
