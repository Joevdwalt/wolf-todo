using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Infrastructure.Markdown;

public sealed record MarkdownTodoItemsResult(IReadOnlyList<TodoItem>? Todos, string? Error)
{
    public bool IsSuccess => Todos is not null;

    public static MarkdownTodoItemsResult Success(IReadOnlyList<TodoItem> todos) => new(todos, null);

    public static MarkdownTodoItemsResult Failure(string error) => new(null, error);
}
