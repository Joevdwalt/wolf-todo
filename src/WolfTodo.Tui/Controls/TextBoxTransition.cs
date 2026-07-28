namespace WolfTodo.Tui.Controls;

public sealed record TextBoxTransition(
    TextBoxState? State,
    TextBoxOutcome Outcome = TextBoxOutcome.Editing);
