namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for Spotify Connect devices (_spotify-connect._tcp)
/// </summary>
public class SpotifyConnectParser : DeviceParserBase
{

    public SpotifyConnectParser(ILogger<SpotifyConnectParser> logger) : base(logger) { }

    public override bool CanParse(DeviceInfo device)
    {
        return HasServiceType(device, "_spotify-connect");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new SpotifyConnectInfo { Available = false };

        try
        {
            var spotifyEndpoints = GetEndpointsByServiceType(device, "_spotify-connect");

            if (!spotifyEndpoints.Any())
            {
                spotifyEndpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_spotify-connect._tcp.local.", 57621)
                };
            }

            var primaryEndpoint = spotifyEndpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // Parse Spotify Connect TXT record fields using helpers
            info.CPath = GetTxtProperty(info.Properties, "CPath");
            info.Version = GetTxtProperty(info.Properties, "VERSION");
            info.Stack = GetTxtProperty(info.Properties, "Stack");
            info.ActiveUser = GetTxtProperty(info.Properties, "activeUser");
            info.PublicKey = GetTxtProperty(info.Properties, "publicKey");
            info.DeviceId = GetTxtProperty(info.Properties, "deviceID");
            info.RemoteName = GetTxtProperty(info.Properties, "remoteName");
            info.TokenType = GetTxtProperty(info.Properties, "tokenType");
            info.ClientId = GetTxtProperty(info.Properties, "clientID");
            info.Scope = GetTxtProperty(info.Properties, "scope");

            info.AccountRequired = GetTxtBooleanProperty(info.Properties, "accountReq");

            info.Available = true;
            Logger.LogInformation("Spotify Connect device detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query Spotify Connect info for {DeviceName}", device.Name);
        }

        return Task.FromResult<object?>(null);
    }
}

public class SpotifyConnectInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? CPath { get; set; }
    public string? Version { get; set; }
    public string? Stack { get; set; }
    public bool AccountRequired { get; set; }
    public string? ActiveUser { get; set; }
    public string? PublicKey { get; set; }
    public string? DeviceId { get; set; }
    public string? RemoteName { get; set; }
    public string? TokenType { get; set; }
    public string? ClientId { get; set; }
    public string? Scope { get; set; }
}
