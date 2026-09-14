namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record TuiStartupArguments(string? TaskCode, string? Error = null)
{
    public static TuiStartupArguments Parse(string[] args) => args switch
    {
        [] => new(TaskCode: null),
        ["--open-task", var code] => new(code),
        ["--open-task"] => new(""),
        _ => new(null, "Usage: wtodo-tui [--open-task <code>]")
    };
}
