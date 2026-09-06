using WolfTodo.Tui.Features.Configuration;

namespace Wolf.Controls.Tests;

internal static class WtodoControlAdapter
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
        theme.Error);

    public static ControlInput ToControlInput(ConsoleKeyInfo key, TuiKeyBindings bindings)
    {
        if (bindings.MatchesMoveUp(key)) return new(ControlInputKind.MoveUp);
        if (bindings.MatchesMoveDown(key)) return new(ControlInputKind.MoveDown);
        if (bindings.MatchesOpen(key)) return new(ControlInputKind.Accept);
        if (bindings.MatchesBack(key)) return new(ControlInputKind.Cancel);
        if (key.Key == ConsoleKey.LeftArrow) return new(ControlInputKind.MoveLeft);
        if (key.Key == ConsoleKey.RightArrow) return new(ControlInputKind.MoveRight);
        if (key.Key == ConsoleKey.Home) return new(ControlInputKind.MoveHome);
        if (key.Key == ConsoleKey.End) return new(ControlInputKind.MoveEnd);
        if (key.Key == ConsoleKey.Backspace) return new(ControlInputKind.Backspace);
        if (key.Key == ConsoleKey.Delete) return new(ControlInputKind.Delete);
        if (key.Key == ConsoleKey.A && key.Modifiers.HasFlag(ConsoleModifiers.Control)) return new(ControlInputKind.SelectAll);
        return ControlInput.FromCharacter(key.KeyChar);
    }
}
