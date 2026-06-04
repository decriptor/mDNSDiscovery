namespace mDNSDiscovery.Cli;

/// <summary>Output rendering modes for discovery results.</summary>
public enum OutputFormat
{
    /// <summary>Human-readable Spectre.Console table.</summary>
    Table,

    /// <summary>A single pretty-printed JSON array of devices.</summary>
    Json,

    /// <summary>Newline-delimited JSON — one device object per line, stream-friendly.</summary>
    Ndjson,
}
