using System.CommandLine;
using mDNSDiscovery.Cli.Output;
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
                Console.WriteLine(DeviceFormatter.FormatServiceTypes(ServiceCatalog.Default, format));
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
