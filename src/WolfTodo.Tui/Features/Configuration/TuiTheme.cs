using Spectre.Console;

namespace WolfTodo.Tui.Features.Configuration;

public sealed record TuiTheme(
    Color Text,
    Color Accent,
    Color Heading,
    Color Border,
    Color Muted,
    Color Success,
    Color Warning,
    Color Error,
    Color Tag,
    Color Date,
    Color Background,
    Color Surface,
    Color Surface2,
    Color SecondaryText,
    Color BorderActive,
    Color AccentBright,
    Color Info);
