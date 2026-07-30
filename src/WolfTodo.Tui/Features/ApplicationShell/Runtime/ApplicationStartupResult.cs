namespace WolfTodo.Tui.Features.ApplicationShell.Runtime;

public sealed record ApplicationStartupResult(ApplicationRuntime? Runtime, int ExitCode)
{
    public bool Succeeded => Runtime is not null;

    public static ApplicationStartupResult Success(ApplicationRuntime runtime) => new(runtime, 0);

    public static ApplicationStartupResult Failure(int exitCode) => new(null, exitCode);
}
