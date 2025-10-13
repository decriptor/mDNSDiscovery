using Zeroconf;

Console.WriteLine("mDNS Device Discovery");
Console.WriteLine("====================");
Console.WriteLine("Scanning for devices on the local network...");
Console.WriteLine("Press Ctrl+C to stop.\n");

var discoveredDevices = new HashSet<string>();

// Common service types to scan for
var serviceTypes = new[]
{
    "_http._tcp.local.",
    "_https._tcp.local.",
    "_printer._tcp.local.",
    "_ipp._tcp.local.",
    "_airplay._tcp.local.",
    "_googlecast._tcp.local.",
    "_homekit._tcp.local.",
    "_hap._tcp.local.",
    "_ssh._tcp.local.",
    "_sftp-ssh._tcp.local.",
    "_smb._tcp.local.",
    "_device-info._tcp.local.",
    "_raop._tcp.local.",
    "_sleep-proxy._udp.local."
};

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n\nStopping discovery...");
    cts.Cancel();
};

try
{
    // Discover all services
    foreach (var serviceType in serviceTypes)
    {
        Console.WriteLine($"\nScanning for {serviceType}...");

        var responses = await ZeroconfResolver.ResolveAsync(
            serviceType,
            scanTime: TimeSpan.FromSeconds(3),
            cancellationToken: cts.Token
        );

        foreach (var response in responses)
        {
            var deviceInfo = $"\n[{serviceType}]";
            deviceInfo += $"\n  Name: {response.DisplayName}";
            deviceInfo += $"\n  Host: {response.IPAddress}";

            if (response.Services.Any())
            {
                foreach (var service in response.Services)
                {
                    deviceInfo += $"\n  Port: {service.Value.Port}";

                    if (service.Value.Properties.Any())
                    {
                        deviceInfo += "\n  Properties:";
                        foreach (var prop in service.Value.Properties)
                        {
                            foreach (var item in prop)
                            {
                                deviceInfo += $"\n    {item.Key} = {item.Value}";
                            }
                        }
                    }
                }
            }

            if (discoveredDevices.Add(deviceInfo))
            {
                Console.WriteLine(deviceInfo);
            }
        }
    }

    Console.WriteLine($"\n\nTotal unique devices found: {discoveredDevices.Count}");
    Console.WriteLine("\nDiscovery complete. Press any key to exit...");
    Console.ReadKey();
}
catch (OperationCanceledException)
{
    Console.WriteLine($"\n\nTotal unique devices found: {discoveredDevices.Count}");
}
