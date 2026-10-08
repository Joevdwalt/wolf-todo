namespace WolfTodo.Tui.Features.Timing;

public interface IPomodoroCompletionNotifier
{
    void Notify(PomodoroCompletion completion, bool playSound);
}
