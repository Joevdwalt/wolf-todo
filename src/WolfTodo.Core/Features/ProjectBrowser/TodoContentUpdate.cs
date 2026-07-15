using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoContentUpdate(
    ImmutableArray<TodoNoteUpdate> Notes,
    ImmutableArray<TodoSubtaskUpdate> Subtasks);

public sealed record TodoNoteUpdate(int? SourceLine, string Text);

public sealed record TodoSubtaskUpdate(int? SourceLine, string Title, bool IsCompleted);
