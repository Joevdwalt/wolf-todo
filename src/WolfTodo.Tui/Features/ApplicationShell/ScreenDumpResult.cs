namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ScreenDumpResult(string? Path, string? Error)
{
    public bool Succeeded => Path is not null;
}
