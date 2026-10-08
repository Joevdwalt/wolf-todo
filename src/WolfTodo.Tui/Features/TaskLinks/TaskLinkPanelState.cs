using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Features.TaskLinks;

public sealed record TaskLinkPanelState(TextBoxState Input, bool IsOpening, string Context);
