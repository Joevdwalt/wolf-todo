using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoTaskEditorState(
    bool IsCreate,
    string? ProjectPath,
    int ProjectPickerIndex,
    int SelectedIndex,
    TodoTaskEditorMode Mode,
    ContentItemKind AddKind,
    bool IsAddingContent,
    string Draft,
    TodoUpdate Values,
    TodoIdentity? Target,
    ImmutableArray<ContentItemDraft> Items,
    string? Error)
{
    public const int FieldCount = 6;

    public bool IsChoosingProject => ProjectPath is null;

    public string ScheduledDate { get; init; } = Values.Schedule?.Date.ToString("yyyy-MM-dd") ?? string.Empty;

    public string ScheduledTime { get; init; } = Values.Schedule?.Time.ToString("HH:mm") ?? string.Empty;

    public bool ScheduleRequired { get; init; }

    public int SelectableCount => FieldCount + Items.Length;

    public bool IsFieldSelected => SelectedIndex < FieldCount;

    public TodoFormField SelectedField => (TodoFormField)Math.Clamp(SelectedIndex, 0, FieldCount - 1);

    public int SelectedContentIndex => SelectedIndex - FieldCount;

    public static TodoTaskEditorState Create(
        string? projectPath,
        bool hasProjects,
        TodoSchedule? schedule = null,
        bool scheduleRequired = false) => new(
        true,
        projectPath,
        0,
        0,
        TodoTaskEditorMode.Browse,
        ContentItemKind.Note,
        false,
        string.Empty,
        new TodoUpdate(string.Empty, null, null, [], null, null, schedule),
        null,
        [],
        hasProjects ? null : "No valid projects are available.")
    {
        ScheduleRequired = scheduleRequired
    };

    public static TodoTaskEditorState Edit(TodoItem todo, TodoIdentity identity) => new(
        false,
        identity.ProjectPath,
        0,
        0,
        TodoTaskEditorMode.Browse,
        ContentItemKind.Note,
        false,
        string.Empty,
        new TodoUpdate(
            todo.Title,
            todo.ExternalReference,
            todo.Priority,
            todo.Tags,
            todo.StartDate,
            todo.DueDate,
            todo.Schedule),
        identity,
        OrderedItems(todo),
        null);

    public TodoTaskUpdate ToUpdate(TodoSchedule? schedule) => new(
        Values with { Schedule = schedule },
        new TodoContentUpdate(
            [.. Items.Select(item => item switch
            {
                ContentNoteDraft note =>
                    (TodoContentItemUpdate)new TodoNoteUpdate(note.SourceLine, note.Text),
                ContentSubtaskDraft subtask =>
                    new TodoSubtaskUpdate(subtask.SourceLine, subtask.Title, subtask.IsCompleted),
                _ => throw new InvalidOperationException("Unsupported todo content item.")
            })]));

    private static ImmutableArray<ContentItemDraft> OrderedItems(TodoItem todo) =>
        [.. todo.Notes
            .Select(note => (ContentItemDraft)new ContentNoteDraft(note.SourceLine, note.Text))
            .Concat(todo.Subtasks.Select(subtask => (ContentItemDraft)new ContentSubtaskDraft(
                subtask.SourceLine,
                subtask.Title,
                subtask.IsCompleted,
                DescendantCount(subtask))))
            .OrderBy(item => item.SourceLine)];

    private static int DescendantCount(TodoItem todo) =>
        todo.Notes.Length + todo.Subtasks.Length + todo.Subtasks.Sum(DescendantCount);
}

public enum TodoTaskEditorMode
{
    Browse,
    ChooseContentType,
    Edit,
    ConfirmRemoval
}

public enum ContentItemKind
{
    Note,
    Subtask
}

public abstract record ContentItemDraft(int? SourceLine);

public sealed record ContentNoteDraft(int? SourceLine, string Text) : ContentItemDraft(SourceLine);

public sealed record ContentSubtaskDraft(
    int? SourceLine,
    string Title,
    bool IsCompleted,
    int DescendantCount) : ContentItemDraft(SourceLine);
