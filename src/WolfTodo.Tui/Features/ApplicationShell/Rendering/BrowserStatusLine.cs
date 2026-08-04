namespace WolfTodo.Tui.Features.ApplicationShell.Rendering;

public sealed record BrowserStatusLine(
    string Text,
    BrowserStatusRole Role = BrowserStatusRole.Default);
