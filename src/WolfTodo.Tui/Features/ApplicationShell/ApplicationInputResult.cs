namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationInputResult(ApplicationRuntime Runtime, bool ShouldExit = false);
