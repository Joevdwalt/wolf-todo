namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ExternalEditorResult(bool Started, string? Error)
{
    public static ExternalEditorResult Success { get; } = new(true, null);

    public static ExternalEditorResult Failure(bool started, string error) => new(started, error);
}
