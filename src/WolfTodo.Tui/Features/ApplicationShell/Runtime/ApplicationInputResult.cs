namespace WolfTodo.Tui.Features.ApplicationShell.Runtime;

public sealed record ApplicationInputResult(ApplicationRuntime Runtime, bool ShouldExit = false);
