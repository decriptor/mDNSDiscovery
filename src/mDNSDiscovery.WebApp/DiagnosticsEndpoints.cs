using System.Net;
using System.Text.RegularExpressions;
using mDNSDiscovery.WebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace mDNSDiscovery.WebApp;

public static class DiagnosticsEndpoints
{
    public static void MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diagnostics").WithTags("Diagnostics");

        group.MapGet("/ping", async ([FromQuery] string? ip, [FromServices] NetworkDiagnosticsService diagnosticsService) =>
        {
            if (string.IsNullOrWhiteSpace(ip) || !IsValidIpOrHostname(ip))
            {
                return Results.BadRequest(new { error = "Invalid IP address or hostname" });
            }

            var result = await diagnosticsService.PingAsync(ip);
            return Results.Json(result);
        });

        group.MapGet("/traceroute", async ([FromQuery] string? ip, [FromServices] NetworkDiagnosticsService diagnosticsService) =>
        {
            if (string.IsNullOrWhiteSpace(ip) || !IsValidIpOrHostname(ip))
            {
                return Results.BadRequest(new { error = "Invalid IP address or hostname" });
            }

            var result = await diagnosticsService.TracerouteAsync(ip);
            return Results.Json(result);
        });

        group.MapGet("/nslookup", async ([FromQuery] string? target, [FromServices] NetworkDiagnosticsService diagnosticsService) =>
        {
            if (string.IsNullOrWhiteSpace(target) || !IsValidIpOrHostname(target))
            {
                return Results.BadRequest(new { error = "Invalid IP address or hostname" });
            }

            var result = await diagnosticsService.NslookupAsync(target);
            return Results.Json(result);
        });

        group.MapGet("/wakeonlan", async ([FromQuery] string? mac, [FromServices] NetworkDiagnosticsService diagnosticsService) =>
        {
            if (string.IsNullOrWhiteSpace(mac) || !IsValidMacAddress(mac))
            {
                return Results.BadRequest(new { error = "Invalid MAC address format. Expected: XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX" });
            }

            var result = await diagnosticsService.WakeOnLanAsync(mac);
            return Results.Json(result);
        });

        group.MapGet("/portscan", async ([FromQuery] string? ip, [FromServices] NetworkDiagnosticsService diagnosticsService) =>
        {
            if (string.IsNullOrWhiteSpace(ip) || !IsValidIpOrHostname(ip))
            {
                return Results.BadRequest(new { error = "Invalid IP address or hostname" });
            }

            var commonPorts = new[] { 21, 22, 23, 25, 53, 80, 110, 143, 443, 445, 548, 631, 3389, 5000, 5353, 8008, 8080, 8443, 9000 };
            var result = await diagnosticsService.PortScanAsync(ip, commonPorts);
            return Results.Json(result);
        });

        group.MapGet("/sslcert", async ([FromQuery] string? ip, [FromServices] NetworkDiagnosticsService diagnosticsService) =>
        {
            if (string.IsNullOrWhiteSpace(ip) || !IsValidIpOrHostname(ip))
            {
                return Results.BadRequest(new { error = "Invalid IP address or hostname" });
            }

            var result = await diagnosticsService.SslCertificateAsync(ip, 443);
            return Results.Json(result);
        });

        group.MapGet("/bandwidth", async ([FromQuery] string? ip, [FromServices] NetworkDiagnosticsService diagnosticsService) =>
        {
            if (string.IsNullOrWhiteSpace(ip) || !IsValidIpOrHostname(ip))
            {
                return Results.BadRequest(new { error = "Invalid IP address or hostname" });
            }

            var result = await diagnosticsService.BandwidthTestAsync(ip);
            return Results.Json(result);
        });
    }

    private static bool IsValidIpOrHostname(string input)
    {
        // Check if it's a valid IP address
        if (IPAddress.TryParse(input, out _))
        {
            return true;
        }

        // Check if it's a valid hostname (including .local mDNS names)
        // Simplified regex to prevent ReDoS (catastrophic backtracking)
        // With timeout protection against malicious input
        try
        {
            var hostnameRegex = new Regex(
                @"^[a-zA-Z0-9][a-zA-Z0-9\-.]{0,251}[a-zA-Z0-9]$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100)  // Timeout prevents ReDoS attacks
            );
            return hostnameRegex.IsMatch(input) && input.Length <= 253;
        }
        catch (RegexMatchTimeoutException)
        {
            // Potential ReDoS attack attempt - reject the input
            return false;
        }
    }

    private static bool IsValidMacAddress(string mac)
    {
        // Accept MAC addresses in formats: XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX
        // With timeout protection
        try
        {
            var macRegex = new Regex(
                @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100)
            );
            return macRegex.IsMatch(mac);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
