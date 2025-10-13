using System.Net.NetworkInformation;
using mDNSDiscovery.WebApp.Services.Parsers;

namespace mDNSDiscovery.WebApp.Services;

public class DeviceQueryService
{
    private readonly IEnumerable<IDeviceParser> _parsers;
    private readonly ILogger<DeviceQueryService> _logger;

    public DeviceQueryService(IEnumerable<IDeviceParser> parsers, ILogger<DeviceQueryService> logger)
    {
        _parsers = parsers;
        _logger = logger;
    }

    public async Task<DeviceExtendedInfo> QueryDeviceAsync(DeviceInfo device)
    {
        var info = new DeviceExtendedInfo { Device = device };

        // Use parser services to query vendor-specific information
        foreach (var parser in _parsers)
        {
            if (parser.CanParse(device))
            {
                try
                {
                    var result = await parser.QueryDeviceAsync(device);

                    // Assign result to appropriate property based on type
                    if (result is ChromecastInfo chromecastInfo)
                    {
                        info.GoogleInfo = chromecastInfo;
                    }
                    else if (result is AirPlayInfo airPlayInfo)
                    {
                        info.AppleInfo = airPlayInfo;
                    }
                    else if (result is PrinterInfo printerInfo)
                    {
                        info.PrinterInfo = printerInfo;
                    }
                    else if (result is HomeKitInfo homeKitInfo)
                    {
                        info.HomeKitInfo = homeKitInfo;
                    }
                    else if (result is SshInfo sshInfo)
                    {
                        info.SshInfo = sshInfo;
                    }
                    else if (result is MatterInfo matterInfo)
                    {
                        info.MatterInfo = matterInfo;
                    }
                    else if (result is SmbInfo smbInfo)
                    {
                        info.SmbInfo = smbInfo;
                    }
                    else if (result is CompanionLinkInfo companionLinkInfo)
                    {
                        info.CompanionLinkInfo = companionLinkInfo;
                    }
                    else if (result is GeneralDeviceInfo generalDeviceInfo)
                    {
                        info.GeneralDeviceInfo = generalDeviceInfo;
                    }
                    else if (result is HueInfo hueInfo)
                    {
                        info.HueInfo = hueInfo;
                    }
                    else if (result is HttpServiceInfo httpServiceInfo)
                    {
                        info.HttpServiceInfo = httpServiceInfo;
                    }
                    else if (result is RaopInfo raopInfo)
                    {
                        info.RaopInfo = raopInfo;
                    }
                    else if (result is ADiskInfo adiskInfo)
                    {
                        info.ADiskInfo = adiskInfo;
                    }
                    else if (result is ScannerInfo scannerInfo)
                    {
                        info.ScannerInfo = scannerInfo;
                    }
                    else if (result is HomebridgeInfo homebridgeInfo)
                    {
                        info.HomebridgeInfo = homebridgeInfo;
                    }
                    else if (result is SpotifyConnectInfo spotifyConnectInfo)
                    {
                        info.SpotifyConnectInfo = spotifyConnectInfo;
                    }
                    else if (result is AirPlayVideoInfo airPlayVideoInfo)
                    {
                        info.AirPlayVideoInfo = airPlayVideoInfo;
                    }
                    else if (result is DaapInfo daapInfo)
                    {
                        info.DaapInfo = daapInfo;
                    }
                    else if (result is AfpInfo afpInfo)
                    {
                        info.AfpInfo = afpInfo;
                    }
                    else if (result is NfsInfo nfsInfo)
                    {
                        info.NfsInfo = nfsInfo;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Parser {ParserType} failed for device {DeviceName}",
                        parser.GetType().Name, device.Name);

                    // Record the failure
                    info.ParserErrors.Add(new ParserError
                    {
                        ParserName = parser.GetType().Name.Replace("Parser", ""),
                        ErrorMessage = ex.Message,
                        FullException = ex.ToString()
                    });
                }
            }
        }

        // Network information (for all devices)
        try
        {
            info.NetworkInfo = await QueryNetworkInfoAsync(device);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query network info for device {DeviceName}", device.Name);
            info.ParserErrors.Add(new ParserError
            {
                ParserName = "Network Info",
                ErrorMessage = ex.Message,
                FullException = ex.ToString()
            });
        }

        // Port scanning
        try
        {
            info.OpenPorts = await ScanCommonPortsAsync(device.IPAddress);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan ports for device {DeviceName}", device.Name);
            info.ParserErrors.Add(new ParserError
            {
                ParserName = "Port Scanner",
                ErrorMessage = ex.Message,
                FullException = ex.ToString()
            });
        }

        return info;
    }

    private async Task<NetworkInfo> QueryNetworkInfoAsync(DeviceInfo device)
    {
        var info = new NetworkInfo();

        try
        {
            // Ping the device
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(device.IPAddress, 1000);

            info.IsReachable = reply.Status == IPStatus.Success;
            info.RoundtripTime = reply.Status == IPStatus.Success ? (long?)reply.RoundtripTime : null;
            info.TTL = reply.Status == IPStatus.Success ? (int?)reply.Options?.Ttl : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ping device {DeviceName}", device.Name);
            info.IsReachable = false;
        }

        return info;
    }

    private async Task<List<int>> ScanCommonPortsAsync(string ipAddress)
    {
        var openPorts = new List<int>();
        var commonPorts = new[] { 21, 22, 23, 80, 443, 445, 631, 3389, 5000, 8008, 8009, 8080, 8443, 9000 };

        var tasks = commonPorts.Select(async port =>
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var connectTask = client.ConnectAsync(ipAddress, port);
                var timeoutTask = Task.Delay(500); // 500ms timeout per port

                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask && client.Connected)
                {
                    return port;
                }
            }
            catch
            {
                // Port is closed or unreachable
            }

            return -1;
        });

        var results = await Task.WhenAll(tasks);
        openPorts.AddRange(results.Where(p => p > 0));

        return openPorts.OrderBy(p => p).ToList();
    }
}

public class DeviceExtendedInfo
{
    public required DeviceInfo Device { get; init; }
    public ChromecastInfo? GoogleInfo { get; set; }
    public AirPlayInfo? AppleInfo { get; set; }
    public PrinterInfo? PrinterInfo { get; set; }
    public HomeKitInfo? HomeKitInfo { get; set; }
    public SshInfo? SshInfo { get; set; }
    public MatterInfo? MatterInfo { get; set; }
    public SmbInfo? SmbInfo { get; set; }
    public CompanionLinkInfo? CompanionLinkInfo { get; set; }
    public GeneralDeviceInfo? GeneralDeviceInfo { get; set; }
    public HueInfo? HueInfo { get; set; }
    public HttpServiceInfo? HttpServiceInfo { get; set; }
    public RaopInfo? RaopInfo { get; set; }
    public ADiskInfo? ADiskInfo { get; set; }
    public ScannerInfo? ScannerInfo { get; set; }
    public HomebridgeInfo? HomebridgeInfo { get; set; }
    public SpotifyConnectInfo? SpotifyConnectInfo { get; set; }
    public AirPlayVideoInfo? AirPlayVideoInfo { get; set; }
    public DaapInfo? DaapInfo { get; set; }
    public AfpInfo? AfpInfo { get; set; }
    public NfsInfo? NfsInfo { get; set; }
    public NetworkInfo? NetworkInfo { get; set; }
    public List<int> OpenPorts { get; set; } = new();
    public List<ParserError> ParserErrors { get; set; } = new();
}

public class ParserError
{
    public required string ParserName { get; set; }
    public required string ErrorMessage { get; set; }
    public required string FullException { get; set; }
}

public class NetworkInfo
{
    public bool IsReachable { get; set; }
    public long? RoundtripTime { get; set; }
    public int? TTL { get; set; }
}
