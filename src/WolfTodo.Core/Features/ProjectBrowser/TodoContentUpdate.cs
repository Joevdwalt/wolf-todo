using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoContentUpdate(
    ImmutableArray<TodoContentItemUpdate> Items);

public abstract record TodoContentItemUpdate(int? SourceLine);

public sealed record TodoNoteUpdate(int? SourceLine, string Text) : TodoContentItemUpdate(SourceLine);

public sealed record TodoSubtaskUpdate(int? SourceLine, string Title, bool IsCompleted) :
    TodoContentItemUpdate(SourceLine);
