# mDNS Discovery — Core extraction, CLI rebuild, tech-debt cleanup

**Date:** 2026-06-04
**Status:** Approved

## Goal

Remove the discovery-logic duplication between the Blazor WebApp and the standalone
console app by extracting a shared `mDNSDiscovery.Core` library, replace the thin
console app with a proper CLI, fix the remaining DI tech debt, and ship an LLM skill
documenting the CLI.

## 1. Tech-debt cleanup (no behavior change)

`WebApp/Program.cs` registers `MdnsDiscoveryService` via a fragile LINQ scan over
`IHostedService`. Replace with the idiomatic shared-instance pattern:

```csharp
builder.Services.AddSingleton<MdnsDiscoveryService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MdnsDiscoveryService>());
```

## 2. `mDNSDiscovery.Core` (new class library, net10.0)

Single source of truth. References **Zeroconf** only.

- `Models/DeviceInfo.cs`, `Models/ServiceEndpoint.cs` — moved verbatim out of
  `MdnsDiscoveryService.cs` into namespace `mDNSDiscovery.Core`.
- `ServiceCatalog.cs` — canonical `IReadOnlyList<string> Default` of the 23 mDNS
  service types (previously duplicated in the console app and the background service).
- `MdnsScanner.cs` — reusable engine:
  - `Task<IReadOnlyList<DeviceInfo>> ScanAsync(IEnumerable<string> serviceTypes, TimeSpan scanTime, CancellationToken)`
  - Owns the create/merge logic (moved from `MdnsDiscoveryService`).
  - Optional `ILogger` (default `NullLogger`) so it works in any host.

## 3. WebApp refactor

- WebApp references Core; add `<Using Include="mDNSDiscovery.Core" />` global using so
  parser/component files that reference `DeviceInfo` compile unchanged.
- `MdnsDiscoveryService` (BackgroundService) keeps its cache + 5-min eviction loop but
  delegates scanning + model construction to `MdnsScanner` and `ServiceCatalog.Default`.
- Device **parsers** stay in WebApp (active-probe HTTP enrichment layer for the Blazor
  UI; not part of the duplicated discovery core).

## 4. CLI (repurpose `src/mDNSDiscovery`)

- **Stack:** System.CommandLine `2.0.8` + Spectre.Console `0.55.2`. References Core.
- **Packaging:** `PackAsTool=true`, `ToolCommandName=mdns`, `RootNamespace=mDNSDiscovery.Cli`.
- **Commands:**
  - `scan` — one-shot discovery, print, exit.
    Options: `--service/-s <type>` (repeatable filter), `--timeout/-t <sec>` (default 5),
    `--format/-f table|json|ndjson` (default table), `--all-types/-a`.
    Exit code `2` when no devices found (scriptability); `0` otherwise.
  - `watch` — live-refreshing table until Ctrl+C, with 5-min stale eviction.
    Options: `--service/-s`, `--timeout/-t`, `--interval/-i <sec>` (default 30).
  - `list-types` — print the service-type catalog. `--format/-f table|json`.
- **Output:** Spectre table (Name · IP · Services · Port); `json` = pretty array via
  System.Text.Json; `ndjson` = one `DeviceInfo` JSON object per line (streamable).
- **Cancellation:** Ctrl+C → cooperative `CancellationToken`.

## 5. Central package versions

Add to `Directory.Packages.props`: `System.CommandLine 2.0.8`, `Spectre.Console 0.55.2`.

## 6. LLM skill

`.claude/skills/mdns-cli/SKILL.md` — commands, options, formats, exit codes, and
copy-paste examples for both `dotnet run --project src/mDNSDiscovery -- <cmd>` and the
installed-tool `mdns <cmd>` forms.

## Non-goals

- Moving the device parsers into Core.
- Changing WebApp UI behavior or the discovery service-type set.
- Publishing the tool to nuget.org.
