using mDNSDiscovery.Cli.Output;

namespace mDNSDiscovery.Cli.Commands;

/// <summary>Shared user-facing warnings for the scan/watch commands.</summary>
public static class CliWarnings
{
    /// <summary>
    /// Warns (on stderr) for each filter that looks like a typo'd shorthand, suggesting the
    /// closest known service type, so a mistyped <c>--service</c> isn't silently scanned as a
    /// bogus type indistinguishable from "nothing found".
    /// </summary>
    public static void WarnUnknownShorthands(string[] filters)
    {
        foreach (var unknown in CliOptions.UnknownShorthands(filters))
        {
            var suggestion = CliOptions.SuggestShorthand(unknown);
            var hint = suggestion is null ? string.Empty : $" Did you mean '{suggestion}'?";
            Consoles.Warn($"'{unknown}' is not a known service type; scanning it literally.{hint}");
        }
    }
}
