using WolfTodo.Tui.Features.ApplicationShell.Runtime;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class TuiApplication
{
    private readonly ApplicationRunner runner;

    public TuiApplication(ApplicationRunner runner)
    {
        this.runner = runner;
    }

    public int Run() => runner.Run();
}
