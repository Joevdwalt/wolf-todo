using Wolf.Controls;

namespace Wolf.Controls.Examples;

internal static class ConsoleInputMapper
{
    public static ControlInput ToControlInput(ConsoleKeyInfo key) => key.Key switch
    {
        ConsoleKey.LeftArrow => new(ControlInputKind.MoveLeft),
        ConsoleKey.RightArrow => new(ControlInputKind.MoveRight),
        ConsoleKey.UpArrow => new(ControlInputKind.MoveUp),
        ConsoleKey.DownArrow => new(ControlInputKind.MoveDown),
        ConsoleKey.Home => new(ControlInputKind.MoveHome),
        ConsoleKey.End => new(ControlInputKind.MoveEnd),
        ConsoleKey.Backspace => new(ControlInputKind.Backspace),
        ConsoleKey.Delete => new(ControlInputKind.Delete),
        ConsoleKey.Enter => new(ControlInputKind.Accept),
        ConsoleKey.Escape => new(ControlInputKind.Cancel),
        ConsoleKey.A when key.Modifiers.HasFlag(ConsoleModifiers.Control) => new(ControlInputKind.SelectAll),
        _ => ControlInput.FromCharacter(key.KeyChar)
    };
}
