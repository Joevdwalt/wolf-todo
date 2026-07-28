namespace WolfTodo.Core.Infrastructure.Markdown;

public sealed record MarkdownNoteLine(
    int SourceLine,
    int Indent,
    string Text,
    bool IsBlank,
    bool IsListItem);
