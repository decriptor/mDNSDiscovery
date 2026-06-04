using System.CommandLine;
using System.Text.Json;
using Spectre.Console;

namespace mDNSDiscovery.Cli.Commands;

/// <summary>Prints the catalog of mDNS service types the tool scans for.</summary>
public static class ListTypesCommand
{
    public static Command Create()
    {
        var formatOption = CliOptions.Format();

        var command = new Command("list-types", "List the mDNS service types in the scan catalog.")
        {
            formatOption,
        };

        command.SetAction(parseResult =>
        {
            var format = parseResult.GetValue(formatOption);

            if (format == OutputFormat.Table)
            {
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("Short Name")
                    .AddColumn("Service Type");

                foreach (var serviceType in ServiceCatalog.Default)
                {
                    table.AddRow(Markup.Escape(ShortName(serviceType)), Markup.Escape(serviceType));
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"[grey]{ServiceCatalog.Default.Count} service type(s).[/]");
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    ServiceCatalog.Default,
                    new JsonSerializerOptions { WriteIndented = format == OutputFormat.Json }));
            }

            return 0;
        });

        return command;
    }

    private static string ShortName(string serviceType)
    {
        var trimmed = serviceType.TrimStart('_');
        var dot = trimmed.IndexOf('.');
        return dot >= 0 ? trimmed[..dot] : trimmed;
    }
}
