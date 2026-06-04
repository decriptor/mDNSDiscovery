using System.Net.Security;
using mDNSDiscovery.WebApp;
using mDNSDiscovery.WebApp.Components;
using mDNSDiscovery.WebApp.Services;
using mDNSDiscovery.WebApp.Services.Parsers;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpClient factory
builder.Services.AddHttpClient();

// Add named HttpClient with short timeout for device queries
builder.Services.AddHttpClient("ShortTimeout")
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(3);
    });

// Add HttpClient that accepts self-signed certificates for local network devices only
// SECURITY: Only bypasses certificate validation for .local mDNS domains
builder.Services.AddHttpClient("InsecureClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
        {
            // Only allow certificate bypass for local network (.local domains)
            // This is necessary for local IoT devices that use self-signed certificates
            if (request.RequestUri?.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) == true ||
                request.RequestUri?.Host.StartsWith("192.168.", StringComparison.Ordinal) == true ||
                request.RequestUri?.Host.StartsWith("10.", StringComparison.Ordinal) == true ||
                (request.RequestUri?.Host.StartsWith("172.", StringComparison.Ordinal) == true &&
                 int.TryParse(request.RequestUri.Host.Split('.')[1], out int second) &&
                 second >= 16 && second <= 31))
            {
                // Accept self-signed certificates for local network devices
                return true;
            }

            // For all other hosts, require valid certificates
            return errors == SslPolicyErrors.None;
        }
    });

// Register device parsers
builder.Services.AddSingleton<IDeviceParser, AirPlayParser>();
builder.Services.AddSingleton<IDeviceParser, AirPlayVideoParser>();
builder.Services.AddSingleton<IDeviceParser, ChromecastParser>();
builder.Services.AddSingleton<IDeviceParser, PrinterParser>();
builder.Services.AddSingleton<IDeviceParser, HomeKitParser>();
builder.Services.AddSingleton<IDeviceParser, SshParser>();
builder.Services.AddSingleton<IDeviceParser, MatterParser>();
builder.Services.AddSingleton<IDeviceParser, SmbParser>();
builder.Services.AddSingleton<IDeviceParser, CompanionLinkParser>();
builder.Services.AddSingleton<IDeviceParser, DeviceInfoParser>();
builder.Services.AddSingleton<IDeviceParser, HueParser>();
builder.Services.AddSingleton<IDeviceParser, HttpServiceParser>();
builder.Services.AddSingleton<IDeviceParser, RaopParser>();
builder.Services.AddSingleton<IDeviceParser, ADiskParser>();
builder.Services.AddSingleton<IDeviceParser, ScannerParser>();
builder.Services.AddSingleton<IDeviceParser, HomebridgeParser>();
builder.Services.AddSingleton<IDeviceParser, SpotifyConnectParser>();
builder.Services.AddSingleton<IDeviceParser, DaapParser>();
builder.Services.AddSingleton<IDeviceParser, AfpParser>();
builder.Services.AddSingleton<IDeviceParser, NfsParser>();

builder.Services.AddSingleton<DeviceIconService>();
builder.Services.AddSingleton<DeviceQueryService>();
builder.Services.AddSingleton<NetworkDiagnosticsService>();
builder.Services.AddSingleton<DeviceCacheService>();
builder.Services.AddSingleton<DeviceHttpClient>();
builder.Services.AddHostedService<CacheCleanupService>();

// Single shared MdnsDiscoveryService instance: hosted (runs the scan loop) and injectable
// into components (which read its live device cache).
builder.Services.AddSingleton<MdnsScanner>();
builder.Services.AddSingleton<MdnsDiscoveryService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MdnsDiscoveryService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map diagnostic API endpoints
app.MapDiagnosticsEndpoints();

app.Run();
