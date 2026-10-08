using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Commands;
using WolfTodo.Tui.Features.Timing;
using WolfTodo.Tui.Features.TodoEditing;
using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.TaskFocus;

public sealed record FocusedTaskView(
    FocusedTaskState State,
    string ProjectTitle,
    ImmutableArray<FocusedTaskItem> Items,
    ImmutableArray<TodoEditorProjectOption> Projects)
{
    public string? GlobalCommand { get; init; }

    public string? GlobalError { get; init; }

    public CommandPaletteView? CommandPalette { get; init; }

    public string? TimerStatus { get; init; }

    public bool TimerIsBright { get; init; }

    public PomodoroPromptState? PomodoroPrompt { get; init; }

    public PomodoroCompletion? PomodoroCompletion { get; init; }

    public RuntimeReloadStatus? ReloadStatus { get; init; }

    public FocusedTaskItem SelectedItem => Items.First(item => item.IsSelected);
}
