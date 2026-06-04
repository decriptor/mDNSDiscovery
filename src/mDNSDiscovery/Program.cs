using System.CommandLine;
using mDNSDiscovery.Cli.Commands;

var root = new RootCommand("mdns — discover mDNS / DNS-SD devices on your local network.")
{
    ScanCommand.Create(),
    WatchCommand.Create(),
    ListTypesCommand.Create(),
};

return await root.Parse(args).InvokeAsync();
