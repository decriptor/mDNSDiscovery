namespace mDNSDiscovery.Core;

/// <summary>
/// The canonical set of mDNS / DNS-SD service types the discovery engine scans for.
/// Single source of truth shared by the web app's background scanner and the CLI.
/// </summary>
public static class ServiceCatalog
{
    /// <summary>
    /// Default service types scanned during discovery.
    /// </summary>
    public static readonly IReadOnlyList<string> Default =
    [
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
        "_sleep-proxy._udp.local.",
        "_sonos._tcp.local.",
        "_spotify-connect._tcp.local.",
        "_roku-rcp._tcp.local.",
        "_ecp._tcp.local.",            // Roku External Control Protocol
        "_hue._tcp.local.",
        "_companion-link._tcp.local.", // Apple TV / HomePod
        "_airpointer._tcp.local.",     // AirPlay pointer device
        "_matter._tcp.local.",         // Matter smart home protocol
        "_matterc._udp.local.",        // Matter commissioning
    ];
}
