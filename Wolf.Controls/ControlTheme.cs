using Spectre.Console;

namespace Wolf.Controls;

/// <summary>Semantic colors used by Wolf.Controls renderers.</summary>
public sealed record ControlTheme(
    Color Text,
    Color Muted,
    Color Accent,
    Color AccentBright,
    Color Border,
    Color BorderActive,
    Color Surface,
    Color Success,
    Color Warning,
    Color Error)
{
    /// <summary>A neutral theme that preserves the terminal's configured palette.</summary>
    public static ControlTheme Default { get; } = new(
        Color.Default,
        Color.Grey,
        Color.Default,
        Color.Default,
        Color.Grey,
        Color.Default,
        Color.Default,
        Color.Green,
        Color.Yellow,
        Color.Red);
}
