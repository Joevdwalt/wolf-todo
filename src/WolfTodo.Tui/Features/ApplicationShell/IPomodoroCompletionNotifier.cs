namespace WolfTodo.Tui.Features.ApplicationShell;

public interface IPomodoroCompletionNotifier
{
    void Notify(PomodoroCompletion completion, bool playSound);
}
