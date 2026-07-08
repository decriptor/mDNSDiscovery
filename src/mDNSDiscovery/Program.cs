using System.CommandLine;
using mDNSDiscovery.Cli.Commands;

var root = new RootCommand("mdns — discover mDNS / DNS-SD devices on your local network.")
{
    ScanCommand.Create(),
    WatchCommand.Create(),
    ListTypesCommand.Create(),
};

// Bare `mdns` shows help and exits 0 (self-discovery) instead of erroring with exit 1.
if (args.Length == 0)
{
    args = ["--help"];
}

return await root.Parse(args).InvokeAsync();
