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
    Color Date);
