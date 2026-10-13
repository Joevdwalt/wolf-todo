using Spectre.Console.Rendering;
using Wolf.Controls.MultilineTextBox;
using Wolf.Controls.SelectList;
using Wolf.Controls.TextBox;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Controls;

public static class TuiControlAdapter
{
    public static ControlTheme ToControlTheme(TuiTheme theme) => new(
        theme.Text,
        theme.Muted,
        theme.Accent,
        theme.AccentBright,
        theme.Border,
        theme.BorderActive,
        theme.Surface,
        theme.Success,
        theme.Warning,
        theme.Error) { SecondaryText = theme.SecondaryText, Heading = theme.Heading };

    public static ControlInput ToTextInput(ConsoleKeyInfo key) => key.Key switch
    {
        ConsoleKey.Escape => new(ControlInputKind.Cancel),
        ConsoleKey.Enter => new(ControlInputKind.Accept),
        ConsoleKey.A when key.Modifiers.HasFlag(ConsoleModifiers.Control) => new(ControlInputKind.SelectAll),
        ConsoleKey.LeftArrow => new(ControlInputKind.MoveLeft),
        ConsoleKey.RightArrow => new(ControlInputKind.MoveRight),
        ConsoleKey.Home => new(ControlInputKind.MoveHome),
        ConsoleKey.End => new(ControlInputKind.MoveEnd),
        ConsoleKey.Backspace => new(ControlInputKind.Backspace),
        ConsoleKey.Delete => new(ControlInputKind.Delete),
        _ => ControlInput.FromCharacter(key.KeyChar)
    };

    public static ControlInput ToMultilineInput(ConsoleKeyInfo key, TuiKeyBindings bindings)
    {
        if (key.Key == ConsoleKey.Escape) return new(ControlInputKind.Cancel);
        if (bindings.MatchesSaveForm(key)) return new(ControlInputKind.Accept);
        if (key.Key == ConsoleKey.Enter) return new(ControlInputKind.InsertNewline);
        return key.Key switch
        {
            ConsoleKey.UpArrow => new(ControlInputKind.MoveUp),
            ConsoleKey.DownArrow => new(ControlInputKind.MoveDown),
            _ => ToTextInput(key)
        };
    }

    public static ControlInput ToListInput(ConsoleKeyInfo key, TuiKeyBindings bindings)
    {
        if (bindings.MatchesBack(key)) return new(ControlInputKind.Cancel);
        if (bindings.MatchesOpen(key)) return new(ControlInputKind.Accept);
        if (bindings.MatchesMoveUp(key)) return new(ControlInputKind.MoveUp);
        if (bindings.MatchesMoveDown(key)) return new(ControlInputKind.MoveDown);
        return ControlInput.FromCharacter(key.KeyChar);
    }

    public static ControlTransition<TextBoxState, TextBoxOutcome> ReduceTextBox(
        TextBoxState state, ConsoleKeyInfo key) =>
        TextBox.Default.Reduce(state, ToTextInput(key));

    public static ControlTransition<MultilineTextBoxState, MultilineTextBoxOutcome> ReduceMultilineTextBox(
        MultilineTextBoxState state, ConsoleKeyInfo key, TuiKeyBindings bindings) =>
        MultilineTextBox.Default.Reduce(state, ToMultilineInput(key, bindings));

    public static ControlTransition<SelectListState, SelectListOutcome> ReduceSelectList(
        SelectListState state, ConsoleKeyInfo key, TuiKeyBindings bindings) =>
        SelectList.Default.Reduce(state, ToListInput(key, bindings));

    public static IRenderable Render(TextBoxState state, TuiTheme theme, ControlConstraints constraints) =>
        TextBox.Default.Render(state, ToControlTheme(theme), constraints);

    public static IRenderable Render(SelectListState state, TuiTheme theme, ControlConstraints constraints) =>
        SelectList.Default.Render(state, ToControlTheme(theme), constraints);

    public static IRenderable Render(
        MultilineTextBoxState state, TuiTheme theme, ControlConstraints constraints, string? saveBinding = null) =>
        MultilineTextBox.Default.Render(
            saveBinding is null ? state : state with { SaveHint = saveBinding },
            ToControlTheme(theme),
            constraints);
}
