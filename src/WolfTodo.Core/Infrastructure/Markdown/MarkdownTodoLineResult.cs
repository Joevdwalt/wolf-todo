namespace WolfTodo.Core.Infrastructure.Markdown;

public sealed record MarkdownTodoLineResult(MarkdownTodoLine? Line, string? Error)
{
    public bool IsTask => Line is not null || Error is not null;

    public static MarkdownTodoLineResult NotATask() => new(null, null);

    public static MarkdownTodoLineResult Success(MarkdownTodoLine line) => new(line, null);

    public static MarkdownTodoLineResult Failure(string error) => new(null, error);
}
