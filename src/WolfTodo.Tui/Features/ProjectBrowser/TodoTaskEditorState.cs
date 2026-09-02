using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoTaskEditorState(
    bool IsCreate,
    string? ProjectPath,
    int ProjectPickerIndex,
    int SelectedIndex,
    TodoTaskEditorMode Mode,
    bool IsAddingSubtask,
    string Content,
    TodoUpdate Values,
    TodoIdentity? Target,
    ImmutableArray<TodoSubtaskDraft> Subtasks,
    string? Error)
{
    public const int FieldCount = 7;
    public const int ContentIndex = FieldCount;

    public bool IsChoosingProject => ProjectPath is null;

    public string ScheduledDate { get; init; } = Values.Schedule?.Date.ToString("yyyy-MM-dd") ?? string.Empty;

    public string ScheduledTime { get; init; } = Values.Schedule?.Time?.ToString("HH:mm") ?? string.Empty;

    public string Duration => Values.Duration is null
        ? string.Empty
        : $"{(int)Values.Duration.Value.TotalMinutes}m";

    public TodoScheduleRequirement ScheduleRequirement { get; init; }

    internal MultilineTextBoxState? ContentTextBox { get; init; }

    internal TextBoxState? FieldTextBox { get; init; }

    internal TextBoxState? SubtaskTextBox { get; init; }

    public bool IsEditingContent => ContentTextBox is not null;

    public bool IsFieldSelected => SelectedIndex < FieldCount;

    public bool IsContentSelected => SelectedIndex == ContentIndex;

    public bool IsSubtaskSelected => SelectedIndex > ContentIndex && SelectedSubtaskIndex < Subtasks.Length;

    public int SelectedSubtaskIndex => SelectedIndex - ContentIndex - 1;

    public int SelectableCount => FieldCount + 1 + Subtasks.Length;

    public TodoFormField SelectedField => (TodoFormField)Math.Clamp(SelectedIndex, 0, FieldCount - 1);

    public static TodoTaskEditorState Create(
        string? projectPath,
        bool hasProjects,
        TodoSchedule? schedule = null,
        TodoScheduleRequirement scheduleRequirement = TodoScheduleRequirement.None,
        TimeSpan? duration = null) => new(
        true,
        projectPath,
        0,
        0,
        TodoTaskEditorMode.Browse,
        false,
        string.Empty,
        new TodoUpdate(string.Empty, null, null, [], null, null, schedule, duration),
        null,
        [],
        hasProjects ? null : "No valid projects are available.")
    {
        ScheduleRequirement = scheduleRequirement
    };

    public static TodoTaskEditorState Edit(TodoItem todo, TodoIdentity identity) => new(
        false,
        identity.ProjectPath,
        0,
        0,
        TodoTaskEditorMode.Browse,
        false,
        string.Join('\n', todo.Notes.Select(note => note.Text)),
        new TodoUpdate(
            todo.Title,
            todo.ExternalReference,
            todo.Priority,
            todo.Tags,
            todo.StartDate,
            todo.DueDate,
            todo.Schedule,
            todo.Duration),
        identity,
        [.. todo.Subtasks.Select(subtask => new TodoSubtaskDraft(
            subtask.SourceLine,
            subtask.Title,
            subtask.IsCompleted,
            DescendantCount(subtask)))],
        null);

    public TodoTaskUpdate ToUpdate(TodoSchedule? schedule) => new(
        Values with { Schedule = schedule },
        new TodoContentUpdate(
            Content,
            [.. Subtasks.Select(subtask => new TodoSubtaskUpdate(
                subtask.SourceLine,
                subtask.Title,
                subtask.IsCompleted))]));

    private static int DescendantCount(TodoItem todo) =>
        todo.Notes.Length + todo.Subtasks.Length + todo.Subtasks.Sum(DescendantCount);
}

public enum TodoTaskEditorMode
{
    Browse,
    Edit,
    ConfirmRemoval
}

public enum TodoScheduleRequirement
{
    None,
    Date,
    DateAndTime
}

public sealed record TodoSubtaskDraft(
    int? SourceLine,
    string Title,
    bool IsCompleted,
    int DescendantCount);
