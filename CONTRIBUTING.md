# Contributing to mDNS Discovery

Thank you for your interest in contributing! This project maintains high code quality standards and welcomes contributions that follow our established patterns.

## Requirements

- ✅ Build without warnings or errors
- ✅ Follow DRY principle (no code duplication)
- ✅ Use DeviceParserBase helpers for all parsers
- ✅ Follow existing patterns
- ✅ Include appropriate error handling

---

## Getting Started

### 1. Set Up Development Environment

```bash
# Install .NET 10.0 SDK
https://dotnet.microsoft.com/download/dotnet/10.0

# Clone the repository
git clone <repository-url>
cd mDNSDiscovery

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
cd src/mDNSDiscovery.WebApp
dotnet run
```

### 2. Understand the Architecture

Read these documents in order:
1. [README.md](README.md) - Overview and features
2. [PARSER_REFACTORING_GUIDE.md](PARSER_REFACTORING_GUIDE.md) - Parser architecture
3. [FINAL_REVIEW_REPORT.md](FINAL_REVIEW_REPORT.md) - Code quality standards

---

## Adding a New Device Parser

### Step-by-Step Guide

**1. Create the parser file:**

Create `src/mDNSDiscovery.WebApp/Services/Parsers/YourDeviceParser.cs`

**2. Use this template:**

```csharp
namespace mDNSDiscovery.WebApp.Services.Parsers;

/// <summary>
/// Parser for YourDevice (_yourservice._tcp)
/// </summary>
public class YourDeviceParser : DeviceParserBase
{
    private readonly IHttpClientFactory? _httpClientFactory; // Optional, if HTTP queries needed

    public YourDeviceParser(ILogger<YourDeviceParser> logger) : base(logger)
    {
        // Add dependencies if needed
    }

    public override bool CanParse(DeviceInfo device)
    {
        // Use HasServiceType helper - REQUIRED
        return HasServiceType(device, "_yourservice");
    }

    public override Task<object?> QueryDeviceAsync(DeviceInfo device, CancellationToken cancellationToken = default)
    {
        var info = new YourDeviceInfo { Available = false };

        try
        {
            // 1. Get endpoints using helper
            var endpoints = GetEndpointsByServiceType(device, "_yourservice");

            if (!endpoints.Any())
            {
                // 2. Create fallback using helper
                endpoints = new List<ServiceEndpoint>
                {
                    CreateFallbackEndpoint(device, "_yourservice._tcp.local.", 8080)
                };
            }

            var primaryEndpoint = endpoints.First();
            info.Properties = primaryEndpoint.Properties;
            info.Port = primaryEndpoint.Port;

            // 3. Parse TXT records using helpers - REQUIRED
            info.Name = GetTxtProperty(info.Properties, "name");
            info.Version = GetTxtProperty(info.Properties, "version", "ver"); // Fallback keys
            info.Enabled = GetTxtBooleanProperty(info.Properties, "enabled");
            info.MaxConnections = GetTxtIntProperty(info.Properties, "maxconn") ?? 10;
            info.Protocols = GetTxtListProperty(info.Properties, "protocols");

            // 4. Query device HTTP endpoint (if applicable)
            // var httpClient = CreateHttpClientWithTimeout(_httpClientFactory, "InsecureClient", 2);
            // var response = await httpClient.GetAsync(..., cancellationToken);

            info.Available = true;
            Logger.LogInformation("YourDevice detected at {IP}:{Port}", device.IPAddress, info.Port);

            return Task.FromResult<object?>(info);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to query YourDevice for {DeviceName}", device.Name);
            return Task.FromResult<object?>(null);
        }
    }
}

public class YourDeviceInfo
{
    public bool Available { get; set; }
    public int Port { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public string? Name { get; set; }
    public string? Version { get; set; }
    public bool Enabled { get; set; }
    public int MaxConnections { get; set; }
    public List<string> Protocols { get; set; } = new();
}
```

**3. Register the parser:**

Add to `src/mDNSDiscovery.WebApp/Program.cs`:
```csharp
builder.Services.AddSingleton<IDeviceParser, YourDeviceParser>();
```

**4. Test your parser:**

```bash
dotnet build
dotnet run
# Navigate to http://localhost:5000
# Verify your device appears
```

**5. Create UI component (optional):**

Create `src/mDNSDiscovery.WebApp/Components/Shared/YourDeviceInfoSection.razor`

---

## Code Standards

### Must Use Base Class Helpers

**REQUIRED patterns:**

```csharp
// ✅ DO: Use HasServiceType
public override bool CanParse(DeviceInfo device)
{
    return HasServiceType(device, "_service");
}

// ❌ DON'T: Manual pattern
public override bool CanParse(DeviceInfo device)
{
    var allServiceTypes = new List<string> { device.ServiceType };
    // ...
}
```

```csharp
// ✅ DO: Use GetTxtProperty
info.Model = GetTxtProperty(info.Properties, "model");

// ❌ DON'T: Manual check
if (info.Properties.ContainsKey("model"))
    info.Model = info.Properties["model"];
```

```csharp
// ✅ DO: Use type-specific helpers
info.Enabled = GetTxtBooleanProperty(info.Properties, "enabled");
info.Port = GetTxtIntProperty(info.Properties, "port") ?? 80;
info.Formats = GetTxtListProperty(info.Properties, "formats");

// ❌ DON'T: Manual parsing
if (info.Properties.ContainsKey("enabled"))
    info.Enabled = info.Properties["enabled"] == "T" || info.Properties["enabled"] == "true";
```

### Documentation Requirements

**All parsers must have:**
1. XML summary comment describing the device type
2. Service type in comment (e.g., `_printer._tcp`)
3. Clear variable names
4. Comments for complex logic

**Example:**
```csharp
/// <summary>
/// Parser for Network Printers (_printer._tcp, _ipp._tcp)
/// </summary>
public class PrinterParser : DeviceParserBase
```

### Error Handling

**All parsers should:**
- Wrap QueryDeviceAsync in try-catch
- Log warnings on failure with `Logger.LogWarning`
- Return null on error
- Log information on success

---

## Testing Your Changes

### Build Test

```bash
dotnet build
# Must succeed with 0 warnings, 0 errors
```

### Pattern Compliance Test

```bash
# Your parser should appear in these results:
grep -l "HasServiceType" src/mDNSDiscovery.WebApp/Services/Parsers/*.cs
grep -l "GetTxtProperty" src/mDNSDiscovery.WebApp/Services/Parsers/*.cs
```

### Manual Testing

1. Run the application
2. Ensure your device appears
3. Click device to see details
4. Verify all parsed information is correct
5. Test with device powered off (should handle gracefully)

---

## Pull Request Process

### Before Submitting

- [ ] Code builds without warnings or errors
- [ ] Follows all code standards above
- [ ] Uses DeviceParserBase helpers appropriately
- [ ] Includes XML documentation
- [ ] Tested manually with actual device
- [ ] No code duplication introduced

### PR Description Should Include

1. **What** - Description of changes
2. **Why** - Reason for changes
3. **How** - Approach taken
4. **Testing** - How you tested
5. **Device** - What device type this supports

### Review Criteria

All PRs are reviewed for:
- Code quality (must maintain A+ grade)
- Pattern compliance
- Documentation completeness
- Error handling
- Performance impact

---

## Development Guidelines

### Naming Conventions

- **Parsers:** `{DeviceType}Parser` (e.g., `PrinterParser`)
- **Info classes:** `{DeviceType}Info` (e.g., `PrinterInfo`)
- **Services:** `{Purpose}Service` (e.g., `DeviceQueryService`)

### File Organization

```
Services/
├── Parsers/
│   ├── DeviceParserBase.cs        # Base class
│   ├── AirPlayParser.cs           # Device-specific parsers
│   └── ...
├── MdnsDiscoveryService.cs        # Core services
└── ...
```

### Logging Levels

- **LogDebug:** Verbose details (TXT records, parsing steps)
- **LogInformation:** Normal operations (device detected)
- **LogWarning:** Recoverable errors (query failed)
- **LogError:** Critical errors (service crashed)

---

## Common Patterns

### Pattern 1: Simple TXT-Only Parser

```csharp
public override Task<object?> QueryDeviceAsync(...)
{
    var info = new MyInfo { Available = true, Port = device.Port, Properties = device.Properties };

    info.Name = GetTxtProperty(info.Properties, "name");
    info.Version = GetTxtProperty(info.Properties, "version");

    Logger.LogInformation("MyDevice detected at {IP}", device.IPAddress);
    return Task.FromResult<object?>(info);
}
```

### Pattern 2: HTTP Query Parser

```csharp
public override async Task<object?> QueryDeviceAsync(...)
{
    try
    {
        var endpoints = GetEndpointsByServiceType(device, "_service");
        if (!endpoints.Any())
        {
            endpoints = new List<ServiceEndpoint>
            {
                CreateFallbackEndpoint(device, "_service._tcp.local.", 80)
            };
        }

        var httpClient = CreateHttpClientWithTimeout(_httpClientFactory, "InsecureClient", 3);
        var response = await httpClient.GetAsync($"http://{device.IPAddress}/info", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            // Parse content...
        }

        return info;
    }
    catch (Exception ex)
    {
        Logger.LogWarning(ex, "Failed to query device");
        return null;
    }
}
```

---

## Resources

### Useful Links

- [mDNS/DNS-SD Specification (RFC 6762)](https://datatracker.ietf.org/doc/html/rfc6762)
- [DNS-Based Service Discovery (RFC 6763)](https://datatracker.ietf.org/doc/html/rfc6763)
- [Service Type Registry](http://www.dns-sd.org/ServiceTypes.html)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)

### mDNS Service Types

Common service types to implement:
- `_http._tcp` - HTTP services
- `_printer._tcp` - IPP printers
- `_airplay._tcp` - AirPlay devices
- `_hap._tcp` - HomeKit accessories
- `_googlecast._tcp` - Chromecast devices
- `_spotify-connect._tcp` - Spotify Connect

Find more at: http://www.dns-sd.org/ServiceTypes.html

---

## Questions?

- Open an issue for questions
- Check existing parsers for examples
- Review the parser guide: [PARSER_REFACTORING_GUIDE.md](PARSER_REFACTORING_GUIDE.md)

---

**Thank you for contributing!** 🙏
