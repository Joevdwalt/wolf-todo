namespace WolfTodo.Tui.Features.ApplicationShell.Runtime;

public sealed class ApplicationRunner
{
    private readonly ApplicationStartup startup;
    private readonly ApplicationSession session;

    public ApplicationRunner(ApplicationStartup startup, ApplicationSession session)
    {
        this.startup = startup;
        this.session = session;
    }

    public int Run()
    {
        var result = startup.Start();
        return result.Succeeded
            ? session.Run(result.Runtime!)
            : result.ExitCode;
    }
}
