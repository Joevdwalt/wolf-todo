using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Infrastructure.Markdown;

public sealed record MarkdownTodoLine(int Indent, TodoItem Todo);
