using Spectre.Console;

namespace mDNSDiscovery.Cli.Output;

/// <summary>
/// Shared Spectre consoles. Progress, warnings, and errors go to <see cref="Error"/> (stderr) so
/// they never pollute stdout, keeping `scan &gt; devices.txt` and piped output clean.
/// </summary>
public static class Consoles
{
    public static readonly IAnsiConsole Error =
        AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(System.Console.Error) });

    /// <summary>Writes a yellow warning line to stderr.</summary>
    public static void Warn(string message) => Error.MarkupLine($"[yellow]Warning:[/] {Markup.Escape(message)}");

    /// <summary>Writes a red error line to stderr.</summary>
    public static void WriteError(string message) => Error.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
}
