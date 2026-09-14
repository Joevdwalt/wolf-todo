using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record TaskLinkPanelState(TextBoxState Input, bool IsOpening, string Context);
