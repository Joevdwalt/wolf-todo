using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Controls;

/// <summary>
/// The single-line editor used for a task's title in the task editor dialog.
/// </summary>
public static class TodoTitleEditor
{
    public static TodoTitleEditorState Create(string title) => new(title, title.Length);

    public static TodoTitleEditorTransition Reduce(TodoTitleEditorState state, ConsoleKeyInfo key)
    {
        var cursor = state.ClampedCursor;
        if (key.Key == ConsoleKey.Escape)
        {
            return new(null, TodoTitleEditorOutcome.Cancelled);
        }

        if (key.Key == ConsoleKey.Enter)
        {
            return new(state, TodoTitleEditorOutcome.Accepted);
        }

        var next = key.Key switch
        {
            ConsoleKey.LeftArrow => state with { Cursor = Math.Max(0, cursor - 1) },
            ConsoleKey.RightArrow => state with { Cursor = Math.Min(state.Text.Length, cursor + 1) },
            ConsoleKey.Home => state with { Cursor = 0 },
            ConsoleKey.End => state with { Cursor = state.Text.Length },
            ConsoleKey.Backspace when cursor > 0 => state with
            {
                Text = state.Text.Remove(cursor - 1, 1),
                Cursor = cursor - 1
            },
            ConsoleKey.Delete when cursor < state.Text.Length => state with
            {
                Text = state.Text.Remove(cursor, 1)
            },
            _ when !char.IsControl(key.KeyChar) => state with
            {
                Text = state.Text.Insert(cursor, key.KeyChar.ToString()),
                Cursor = cursor + 1
            },
            _ => state
        };
        return new(next);
    }

    public static string DisplayText(TodoTitleEditorState state)
    {
        var cursor = state.ClampedCursor;
        return state.Text[..cursor] + "_" + state.Text[cursor..];
    }

    public static IRenderable CreateRenderable(TodoTitleEditorState state, TuiTheme theme) =>
        new Text(DisplayText(state), new Style(theme.AccentBright, decoration: Decoration.Bold));
}

public sealed record TodoTitleEditorState(string Text, int Cursor)
{
    public int ClampedCursor => Math.Clamp(Cursor, 0, Text.Length);
}

public sealed record TodoTitleEditorTransition(
    TodoTitleEditorState? State,
    TodoTitleEditorOutcome Outcome = TodoTitleEditorOutcome.Editing);

public enum TodoTitleEditorOutcome
{
    Editing,
    Accepted,
    Cancelled
}
