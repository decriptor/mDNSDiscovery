using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace mDNSDiscovery.WebApp.Services;

public class NetworkDiagnosticsService
{
    private readonly ILogger<NetworkDiagnosticsService> _logger;

    public NetworkDiagnosticsService(ILogger<NetworkDiagnosticsService> logger)
    {
        _logger = logger;
    }

    public async Task<DiagnosticResult> WakeOnLanAsync(string macAddress)
    {
        var result = new DiagnosticResult { Command = $"Wake-on-LAN to {macAddress}" };
        var output = new StringBuilder();

        try
        {
            // Parse and validate MAC address
            var mac = macAddress.Replace(":", "").Replace("-", "").Replace(" ", "");

            if (mac.Length != 12 || !mac.All(c => "0123456789ABCDEFabcdef".Contains(c)))
            {
                result.Output = "Invalid MAC address format. Expected format: XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX";
                result.Success = false;
                return result;
            }

            // Convert MAC address to bytes
            byte[] macBytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = Convert.ToByte(mac.Substring(i * 2, 2), 16);
            }

            // Build magic packet
            byte[] magicPacket = new byte[102];

            // First 6 bytes are 0xFF
            for (int i = 0; i < 6; i++)
            {
                magicPacket[i] = 0xFF;
            }

            // MAC address repeated 16 times
            for (int i = 1; i <= 16; i++)
            {
                Array.Copy(macBytes, 0, magicPacket, i * 6, 6);
            }

            // Send magic packet via UDP broadcast
            using var client = new UdpClient();
            client.EnableBroadcast = true;

            // Send to port 9 (standard WoL port) and port 7 (some devices)
            await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, 9));
            await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, 7));

            output.AppendLine($"Magic packet sent to MAC address: {macAddress}");
            output.AppendLine($"Broadcast to ports 9 and 7");
            output.AppendLine();
            output.AppendLine("Note: The device must support Wake-on-LAN and be configured to respond to magic packets.");
            output.AppendLine("If the device doesn't wake up, check:");
            output.AppendLine("  • Wake-on-LAN is enabled in BIOS/UEFI");
            output.AppendLine("  • Network adapter WoL settings are enabled");
            output.AppendLine("  • Device is connected to power");
            output.AppendLine("  • Device is on the same network segment");

            result.Output = output.ToString();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Wake-on-LAN packet to {MacAddress}", macAddress);
            result.Output = $"Error: {ex.Message}";
            result.Success = false;
        }

        return result;
    }

    public async Task<DiagnosticResult> PingAsync(string ipAddress, int count = 4)
    {
        var result = new DiagnosticResult { Command = $"ping -c {count} {ipAddress}" };
        var output = new StringBuilder();

        try
        {
            using var ping = new Ping();
            output.AppendLine($"Pinging {ipAddress} with {count} packets...\n");

            for (int i = 0; i < count; i++)
            {
                try
                {
                    output.Append($"[{i + 1}/{count}] Sending ping... ");
                    var reply = await ping.SendPingAsync(ipAddress, 5000);

                    if (reply.Status == IPStatus.Success)
                    {
                        output.AppendLine($"Reply from {reply.Address}: bytes={reply.Buffer.Length} time={reply.RoundtripTime}ms TTL={reply.Options?.Ttl}");
                    }
                    else
                    {
                        output.AppendLine($"Request timed out or failed: {reply.Status}");
                    }

                    if (i < count - 1)
                        await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    output.AppendLine($"Error: {ex.Message}");
                }
            }

            output.AppendLine($"\nPing complete: {count} packets sent.");
            result.Output = output.ToString();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ping {IpAddress}", ipAddress);
            result.Output = $"Error: {ex.Message}";
            result.Success = false;
        }

        return result;
    }

    public async Task<DiagnosticResult> TracerouteAsync(string ipAddress, int maxHops = 30)
    {
        var result = new DiagnosticResult { Command = $"traceroute to {ipAddress}, {maxHops} hops max" };
        var output = new StringBuilder();

        try
        {
            output.AppendLine($"Tracing route to {ipAddress} with maximum of {maxHops} hops:\n");

            using var ping = new Ping();
            var options = new PingOptions(1, true); // Start with TTL=1, don't fragment

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                options.Ttl = ttl;
                output.Append($"{ttl,3}  ");

                try
                {
                    var reply = await ping.SendPingAsync(ipAddress, 5000, new byte[32], options);

                    if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
                    {
                        var hopAddress = reply.Address.ToString();
                        output.Append($"{reply.RoundtripTime,4} ms  {hopAddress}");

                        // Try to get hostname
                        try
                        {
                            var hostEntry = await Dns.GetHostEntryAsync(reply.Address);
                            if (!string.IsNullOrEmpty(hostEntry.HostName) && hostEntry.HostName != hopAddress)
                            {
                                output.Append($" ({hostEntry.HostName})");
                            }
                        }
                        catch
                        {
                            // Hostname lookup failed, just use IP
                        }

                        output.AppendLine();

                        // Reached destination
                        if (reply.Status == IPStatus.Success)
                        {
                            output.AppendLine($"\nTrace complete.");
                            break;
                        }
                    }
                    else if (reply.Status == IPStatus.TimedOut)
                    {
                        output.AppendLine("  *  *  *  Request timed out");
                    }
                    else
                    {
                        output.AppendLine($"  {reply.Status}");
                    }
                }
                catch (Exception ex)
                {
                    output.AppendLine($"  Error: {ex.Message}");
                }

                // Small delay between hops
                await Task.Delay(100);
            }

            result.Output = output.ToString();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to traceroute {IpAddress}", ipAddress);
            result.Output = $"Error: {ex.Message}";
            result.Success = false;
        }

        return result;
    }

    public async Task<DiagnosticResult> NslookupAsync(string target)
    {
        var result = new DiagnosticResult { Command = $"Reverse DNS lookup for {target}" };
        var output = new StringBuilder();

        try
        {
            // Try to determine if target is an IP address or hostname
            bool isIpAddress = IPAddress.TryParse(target, out var ipAddr);

            if (isIpAddress && ipAddr != null)
            {
                // Reverse DNS lookup - more useful for mDNS devices
                output.AppendLine($"Reverse DNS Lookup for IP: {ipAddr}\n");
                output.AppendLine("This queries DNS servers to find hostnames associated with this IP address.");
                output.AppendLine("For mDNS devices (*.local), this may reveal additional DNS hostnames if registered.\n");

                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync(ipAddr);

                    output.AppendLine($"Address:   {ipAddr}");
                    output.AppendLine($"Hostname:  {hostEntry.HostName}");

                    if (hostEntry.Aliases.Any())
                    {
                        output.AppendLine("\nAliases:");
                        foreach (var alias in hostEntry.Aliases)
                        {
                            output.AppendLine($"  {alias}");
                        }
                    }

                    // Check if hostname is mDNS (.local) or regular DNS
                    if (hostEntry.HostName.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
                    {
                        output.AppendLine("\nℹ️  This is an mDNS (.local) name resolved by multicast DNS on the local network.");
                    }
                    else
                    {
                        output.AppendLine("\nℹ️  This device has a DNS hostname, suggesting it may be registered in DNS or DHCP.");
                    }

                    result.Output = output.ToString();
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    output.AppendLine($"Reverse DNS lookup failed: {ex.Message}");
                    output.AppendLine("\nThis is normal for devices that only use mDNS (*.local names).");
                    output.AppendLine("The device may not be registered in DNS or may not have a PTR record.");

                    result.Output = output.ToString();
                    result.Success = false;
                }
            }
            else
            {
                // Forward DNS lookup for hostname
                output.AppendLine($"DNS Lookup for: {target}\n");

                // Check if it's an mDNS name
                if (target.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
                {
                    output.AppendLine("ℹ️  This is an mDNS (.local) name.");
                    output.AppendLine("mDNS names are resolved via multicast on the local network, not traditional DNS.\n");
                }

                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync(target);

                    output.AppendLine($"Name:      {hostEntry.HostName}");

                    if (hostEntry.Aliases.Any())
                    {
                        output.AppendLine("\nAliases:");
                        foreach (var alias in hostEntry.Aliases)
                        {
                            output.AppendLine($"  {alias}");
                        }
                    }

                    output.AppendLine("\nAddresses:");
                    var ipv4Addresses = hostEntry.AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToList();
                    var ipv6Addresses = hostEntry.AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6).ToList();

                    if (ipv4Addresses.Any())
                    {
                        output.AppendLine("  IPv4:");
                        foreach (var address in ipv4Addresses)
                        {
                            output.AppendLine($"    {address}");
                        }
                    }

                    if (ipv6Addresses.Any())
                    {
                        output.AppendLine("  IPv6:");
                        foreach (var address in ipv6Addresses)
                        {
                            output.AppendLine($"    {address}");
                        }
                    }

                    result.Output = output.ToString();
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    output.AppendLine($"DNS lookup failed: {ex.Message}");

                    if (target.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
                    {
                        output.AppendLine("\nFor mDNS devices, the OS's mDNS resolver integration varies.");
                        output.AppendLine("The device is still reachable via mDNS on the local network.");
                    }

                    result.Output = output.ToString();
                    result.Success = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lookup {Target}", target);
            result.Output = $"Error: {ex.Message}";
            result.Success = false;
        }

        return result;
    }

    public async Task<DiagnosticResult> PortScanAsync(string ipAddress, int[] ports)
    {
        var result = new DiagnosticResult { Command = $"Port scan for {ipAddress}" };
        var output = new StringBuilder();

        try
        {
            output.AppendLine($"Scanning {ports.Length} common ports on {ipAddress}...\n");

            var tasks = ports.Select(async port =>
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    using var client = new TcpClient();
                    var connectTask = client.ConnectAsync(ipAddress, port);
                    var timeoutTask = Task.Delay(2000);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == connectTask && client.Connected)
                    {
                        stopwatch.Stop();
                        return new { Port = port, Open = true, Time = stopwatch.ElapsedMilliseconds, Service = GetServiceName(port) };
                    }
                }
                catch
                {
                    // Port closed or unreachable
                }

                stopwatch.Stop();
                return new { Port = port, Open = false, Time = stopwatch.ElapsedMilliseconds, Service = GetServiceName(port) };
            });

            var results = await Task.WhenAll(tasks);

            var openPorts = results.Where(r => r.Open).OrderBy(r => r.Port).ToList();
            var closedPorts = results.Where(r => !r.Open).OrderBy(r => r.Port).ToList();

            if (openPorts.Any())
            {
                output.AppendLine($"✅ Open Ports ({openPorts.Count}):");
                foreach (var portResult in openPorts)
                {
                    output.AppendLine($"  Port {portResult.Port,5} - {portResult.Service,-20} (response time: {portResult.Time}ms)");
                }
                output.AppendLine();
            }

            output.AppendLine($"❌ Closed/Filtered Ports: {closedPorts.Count}");
            output.AppendLine($"\nTotal ports scanned: {ports.Length}");
            output.AppendLine($"Open: {openPorts.Count}, Closed/Filtered: {closedPorts.Count}");

            result.Output = output.ToString();
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan ports on {IpAddress}", ipAddress);
            result.Output = $"Error: {ex.Message}";
            result.Success = false;
        }

        return result;
    }

    public async Task<DiagnosticResult> SslCertificateAsync(string ipAddress, int port = 443)
    {
        var result = new DiagnosticResult { Command = $"SSL Certificate inspection for {ipAddress}:{port}" };
        var output = new StringBuilder();

        try
        {
            output.AppendLine($"Connecting to {ipAddress}:{port} to retrieve SSL/TLS certificate...\n");

            X509Certificate2? serverCertificate = null;
            SslPolicyErrors? sslErrors = null;
            string? hostname = null;

            // Try to resolve hostname for SNI (Server Name Indication)
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                hostname = hostEntry.HostName;
                if (!string.IsNullOrEmpty(hostname) && hostname != ipAddress)
                {
                    output.AppendLine($"Using hostname '{hostname}' for SNI\n");
                }
                else
                {
                    hostname = ipAddress;
                }
            }
            catch
            {
                hostname = ipAddress;
            }

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ipAddress, port);
            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                output.AppendLine($"❌ Connection timeout after 5 seconds");
                output.AppendLine("\nPossible reasons:");
                output.AppendLine("  • Port is not open or not an SSL/TLS service");
                output.AppendLine("  • Firewall blocking the connection");
                output.AppendLine("  • Host is not reachable");
                result.Output = output.ToString();
                result.Success = false;
                return result;
            }

            if (!client.Connected)
            {
                output.AppendLine($"❌ Failed to connect to {ipAddress}:{port}");
                output.AppendLine("\nPossible reasons:");
                output.AppendLine("  • Port is not open");
                output.AppendLine("  • Service is not running");
                result.Output = output.ToString();
                result.Success = false;
                return result;
            }

            using var sslStream = new SslStream(client.GetStream(), false, (sender, certificate, chain, errors) =>
            {
                if (certificate != null)
                {
                    serverCertificate = new X509Certificate2(certificate);
                    sslErrors = errors;
                }
                return true; // Accept all certificates for inspection
            });

            try
            {
                // Use hostname for SNI if available
                await sslStream.AuthenticateAsClientAsync(hostname);
            }
            catch (Exception authEx)
            {
                // Authentication may fail, but we might still have captured the certificate
                if (serverCertificate == null)
                {
                    output.AppendLine($"❌ SSL/TLS handshake failed: {authEx.Message}");
                    output.AppendLine("\nPossible reasons:");
                    output.AppendLine("  • Port is not an SSL/TLS service");
                    output.AppendLine("  • Server requires specific protocol version");
                    output.AppendLine("  • Server requires client certificate");
                    result.Output = output.ToString();
                    result.Success = false;
                    return result;
                }
            }

            if (serverCertificate != null)
            {
                output.AppendLine("📜 Certificate Information:");
                output.AppendLine($"  Subject:       {serverCertificate.Subject}");
                output.AppendLine($"  Issuer:        {serverCertificate.Issuer}");
                output.AppendLine($"  Valid From:    {serverCertificate.NotBefore:yyyy-MM-dd HH:mm:ss}");
                output.AppendLine($"  Valid Until:   {serverCertificate.NotAfter:yyyy-MM-dd HH:mm:ss}");

                var daysUntilExpiry = (serverCertificate.NotAfter - DateTime.Now).Days;
                if (daysUntilExpiry < 0)
                {
                    output.AppendLine($"  Status:        ❌ EXPIRED ({Math.Abs(daysUntilExpiry)} days ago)");
                }
                else if (daysUntilExpiry < 30)
                {
                    output.AppendLine($"  Status:        ⚠️  Expires soon ({daysUntilExpiry} days)");
                }
                else
                {
                    output.AppendLine($"  Status:        ✅ Valid ({daysUntilExpiry} days remaining)");
                }

                output.AppendLine($"  Serial Number: {serverCertificate.SerialNumber}");
                output.AppendLine($"  Thumbprint:    {serverCertificate.Thumbprint}");
                output.AppendLine($"  Signature:     {serverCertificate.SignatureAlgorithm.FriendlyName}");
                var keySize = serverCertificate.GetRSAPublicKey()?.KeySize ??
                             serverCertificate.GetECDsaPublicKey()?.KeySize ?? 0;
                output.AppendLine($"  Public Key:    {serverCertificate.PublicKey.Oid.FriendlyName} ({keySize} bits)");

                // DNS names
                var subjectAltNames = serverCertificate.Extensions
                    .OfType<X509SubjectAlternativeNameExtension>()
                    .FirstOrDefault();

                if (subjectAltNames != null)
                {
                    output.AppendLine($"\n  DNS Names:");
                    foreach (var dnsName in subjectAltNames.EnumerateDnsNames())
                    {
                        output.AppendLine($"    • {dnsName}");
                    }
                }

                // SSL policy errors
                if (sslErrors.HasValue && sslErrors.Value != SslPolicyErrors.None)
                {
                    output.AppendLine($"\n⚠️  SSL Policy Errors:");
                    if ((sslErrors.Value & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
                        output.AppendLine("  • Certificate chain validation failed");
                    if ((sslErrors.Value & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
                        output.AppendLine("  • Certificate name does not match hostname");
                    if ((sslErrors.Value & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
                        output.AppendLine("  • Certificate not available");
                }

                result.Output = output.ToString();
                result.Success = true;
            }
            else
            {
                output.AppendLine("❌ No certificate received from server");
                result.Output = output.ToString();
                result.Success = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inspect SSL certificate for {IpAddress}:{Port}", ipAddress, port);
            output.AppendLine($"❌ Failed to retrieve certificate: {ex.Message}");
            output.AppendLine("\nPossible reasons:");
            output.AppendLine("  • Port is not an SSL/TLS service");
            output.AppendLine("  • Server requires SNI (Server Name Indication)");
            output.AppendLine("  • Connection timeout or firewall blocking");
            result.Output = output.ToString();
            result.Success = false;
        }

        return result;
    }

    public async Task<DiagnosticResult> BandwidthTestAsync(string ipAddress)
    {
        var result = new DiagnosticResult { Command = $"Bandwidth test to {ipAddress}" };
        var output = new StringBuilder();

        try
        {
            output.AppendLine($"Testing connection latency and throughput to {ipAddress}...\n");

            // Test 1: Latency (multiple pings)
            output.AppendLine("📊 Latency Test (10 pings):");
            var latencies = new List<long>();

            using var ping = new Ping();
            for (int i = 0; i < 10; i++)
            {
                var reply = await ping.SendPingAsync(ipAddress, 2000);
                if (reply.Status == IPStatus.Success)
                {
                    latencies.Add(reply.RoundtripTime);
                }
                await Task.Delay(100);
            }

            if (latencies.Any())
            {
                output.AppendLine($"  Min:     {latencies.Min()} ms");
                output.AppendLine($"  Max:     {latencies.Max()} ms");
                output.AppendLine($"  Average: {latencies.Average():F2} ms");
                output.AppendLine($"  Jitter:  {CalculateJitter(latencies):F2} ms");

                // Quality assessment
                var avgLatency = latencies.Average();
                if (avgLatency < 10)
                    output.AppendLine($"  Quality: ✅ Excellent (LAN)");
                else if (avgLatency < 50)
                    output.AppendLine($"  Quality: ✅ Good");
                else if (avgLatency < 100)
                    output.AppendLine($"  Quality: ⚠️  Fair");
                else
                    output.AppendLine($"  Quality: ❌ Poor");
            }
            else
            {
                output.AppendLine("  ❌ All ping requests failed");
            }

            output.AppendLine("\nℹ️  Note: Full bandwidth testing requires a compatible server.");
            output.AppendLine("   For accurate bandwidth tests, use tools like iperf3 with a test server.");

            result.Output = output.ToString();
            result.Success = latencies.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed bandwidth test to {IpAddress}", ipAddress);
            result.Output = $"Error: {ex.Message}";
            result.Success = false;
        }

        return result;
    }

    private static string GetServiceName(int port)
    {
        return port switch
        {
            21 => "FTP",
            22 => "SSH",
            23 => "Telnet",
            25 => "SMTP",
            53 => "DNS",
            80 => "HTTP",
            110 => "POP3",
            143 => "IMAP",
            443 => "HTTPS",
            445 => "SMB",
            548 => "AFP",
            631 => "IPP (Printing)",
            3389 => "RDP",
            5000 => "UPnP",
            5353 => "mDNS",
            8008 => "HTTP Alt",
            8080 => "HTTP Proxy",
            8443 => "HTTPS Alt",
            9000 => "Generic",
            _ => "Unknown"
        };
    }

    private static double CalculateJitter(List<long> latencies)
    {
        if (latencies.Count < 2) return 0;

        var differences = new List<double>();
        for (int i = 1; i < latencies.Count; i++)
        {
            differences.Add(Math.Abs(latencies[i] - latencies[i - 1]));
        }

        return differences.Average();
    }
}

public class DiagnosticResult
{
    public string Command { get; set; } = "";
    public string Output { get; set; } = "";
    public bool Success { get; set; }
}
