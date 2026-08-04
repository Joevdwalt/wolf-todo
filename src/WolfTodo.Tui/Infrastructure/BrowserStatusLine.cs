namespace WolfTodo.Tui.Infrastructure;

public sealed record BrowserStatusLine(
    string Text,
    BrowserStatusRole Role = BrowserStatusRole.Default);
