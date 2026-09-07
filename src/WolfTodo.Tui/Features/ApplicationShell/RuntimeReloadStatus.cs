namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record RuntimeReloadStatus(string Message, bool IsError, bool DismissOnInput);
