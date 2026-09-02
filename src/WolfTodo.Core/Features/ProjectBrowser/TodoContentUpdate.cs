using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoContentUpdate
{
    public TodoContentUpdate(string content, ImmutableArray<TodoSubtaskUpdate> subtasks)
    {
        Content = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        Subtasks = subtasks;
    }

    public string Content { get; }

    public ImmutableArray<TodoSubtaskUpdate> Subtasks { get; }
}

public sealed record TodoSubtaskUpdate(int? SourceLine, string Title, bool IsCompleted);
