using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoContentEditorState(
    TodoIdentity Target,
    string TodoTitle,
    ContentEditorFocus Focus,
    int NoteIndex,
    int SubtaskIndex,
    ContentEditorMode Mode,
    bool IsAdding,
    string Draft,
    ImmutableArray<ContentNoteDraft> Notes,
    ImmutableArray<ContentSubtaskDraft> Subtasks,
    string? Error)
{
    public static TodoContentEditorState Create(TodoIdentity target, TodoItem todo) => new(
        target,
        todo.Title,
        ContentEditorFocus.Notes,
        0,
        0,
        ContentEditorMode.Browse,
        false,
        string.Empty,
        [.. todo.Notes.Select(note => new ContentNoteDraft(note.SourceLine, note.Text))],
        [.. todo.Subtasks.Select(subtask => new ContentSubtaskDraft(
            subtask.SourceLine,
            subtask.Title,
            subtask.IsCompleted,
            DescendantCount(subtask)))],
        null);

    public TodoContentUpdate ToUpdate() => new(
        [.. Notes.Select(note => new TodoNoteUpdate(note.SourceLine, note.Text))],
        [.. Subtasks.Select(todo => new TodoSubtaskUpdate(todo.SourceLine, todo.Title, todo.IsCompleted))]);

    private static int DescendantCount(TodoItem todo) =>
        todo.Notes.Length + todo.Subtasks.Length + todo.Subtasks.Sum(DescendantCount);
}

public sealed record ContentNoteDraft(int? SourceLine, string Text);

public sealed record ContentSubtaskDraft(
    int? SourceLine,
    string Title,
    bool IsCompleted,
    int DescendantCount);
