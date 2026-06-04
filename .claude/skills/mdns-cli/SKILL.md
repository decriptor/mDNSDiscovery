---
name: mdns-cli
description: Use when discovering, listing, or inspecting mDNS / DNS-SD ("Bonjour"/Zeroconf) devices on the local network with this repo's `mdns` CLI — e.g. "what's advertising on my network", "find the AirPlay/Chromecast/printer devices", "scan for a service type", "watch the network", or scripting device discovery into JSON/NDJSON.
---

# mdns CLI

A command-line mDNS / DNS-SD device-discovery tool. Lives in `src/mDNSDiscovery`
(project `mDNSDiscovery.Cli`, assembly/command `mdns`) and shares its discovery
engine with the Blazor web app via `mDNSDiscovery.Core`.

## Running it

From the repo, no install needed:

```bash
dotnet run --project src/mDNSDiscovery -- <command> [options]
```

Everything after `--` goes to the CLI. As an installed .NET tool the same commands
are just `mdns <command> [options]` (see "Install as a tool" below).

## Commands

| Command | What it does |
|---|---|
| `scan` | Discover devices once, print, then exit. |
| `watch` | Keep scanning and refresh the view until Ctrl+C. |
| `list-types` | Print the catalog of mDNS service types that get scanned. |

Run `... -- <command> --help` for the authoritative option list.

## Options

| Option | Alias | Commands | Default | Meaning |
|---|---|---|---|---|
| `--service` | `-s` | scan, watch | (full catalog) | Service type to scan for; **repeatable**. Shorthand (`airplay`) or full type (`_airplay._tcp.local.`). Omit to scan the whole catalog. |
| `--timeout` | `-t` | scan, watch | scan `5`, watch `2` | Seconds to listen for responses in each scan window. |
| `--interval` | `-i` | watch | `30` | Seconds to wait between scan cycles. |
| `--format` | `-f` | all | `table` | `table`, `json`, or `ndjson`. |

All service types are resolved within a **single** scan window, so `--timeout` is the
total listen time, not per-type.

## Output formats

- `table` — human-readable Spectre.Console table (Name · IP · Services · Port). `watch`
  renders this as a live-updating table.
- `json` — one pretty-printed JSON array of device objects (full TXT records included).
- `ndjson` — one compact JSON device object per line; stream-friendly. In `watch`, any
  non-`table` format streams NDJSON, re-emitting the current device set each cycle.

## Exit codes (scan)

- `0` — at least one device found.
- `2` — no devices found (lets scripts branch on "nothing there").
- `130` — interrupted with Ctrl+C.

## Examples

```bash
# One-shot scan of the whole catalog, pretty table
dotnet run --project src/mDNSDiscovery -- scan

# Only AirPlay + Chromecast, 8-second window
dotnet run --project src/mDNSDiscovery -- scan -s airplay -s googlecast -t 8

# Machine-readable, piped into jq (suppress build noise with -v q or 2>/dev/null)
dotnet run --project src/mDNSDiscovery -- scan -f json 2>/dev/null | jq '.[].name'

# Stream NDJSON of printers as they appear, one device per line
dotnet run --project src/mDNSDiscovery -- watch -s printer -s ipp -f ndjson

# Live dashboard of everything, rescanning every 10s
dotnet run --project src/mDNSDiscovery -- watch -i 10

# What service types does it know about?
dotnet run --project src/mDNSDiscovery -- list-types
```

## Install as a tool

The project is packaged as a .NET tool (`ToolCommandName=mdns`):

```bash
dotnet pack -c Release src/mDNSDiscovery -o ./nupkg
dotnet tool install --global --add-source ./nupkg mDNSDiscovery.Cli
mdns scan            # then invoke directly
```

Use `--tool-path <dir>` instead of `--global` for an isolated install, and
`dotnet tool uninstall --global mDNSDiscovery.Cli` to remove it.

## Notes

- Needs a real local network; results depend on what's advertising via mDNS at scan time.
- To change the set of scanned service types, edit `ServiceCatalog.Default` in
  `src/mDNSDiscovery.Core/ServiceCatalog.cs` — the CLI and the web app both use it.
- The CLI reports raw TXT-record properties. Friendly per-vendor parsing
  (AirPlay/HomeKit/etc.) lives in the web app, not the CLI.
