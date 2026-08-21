using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;

namespace WolfTodo.Tui.Features.ProjectBrowser.Controls;

public sealed class TodoListItem : ITuiComponent<TodoListItemState, TodoListItemOutcome>
{
    private readonly TodoRowRenderer rowRenderer = TodoRowRenderer.Default;

    public static TodoListItem Default { get; } = new();

    public TuiComponentTransition<TodoListItemState, TodoListItemOutcome> Reduce(
        TodoListItemState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings) =>
        bindings.MatchesToggleTodoSelection(key) && state.Row.Identity is not null
            ? ToggleMark(state)
            : new(state, TodoListItemOutcome.Editing);

    public TuiComponentTransition<TodoListItemState, TodoListItemOutcome> ToggleMark(
        TodoListItemState state) => new(
        state with { Row = state.Row with { IsMarked = !state.Row.IsMarked } },
        TodoListItemOutcome.MarkToggled);

    public int Measure(TodoListItemState state, TuiComponentConstraints constraints) =>
        state.Row.Todo?.Tags.Length > 0 ? 2 : 1;

    public IRenderable Render(
        TodoListItemState state,
        TuiTheme theme,
        TuiComponentConstraints constraints) => new Rows(RenderLines(state, theme));

    public IReadOnlyList<IRenderable> RenderLines(TodoListItemState state, TuiTheme theme)
    {
        var layout = rowRenderer.Columns(state.ContentWidth, state.IncludeProject);
        var lines = new List<IRenderable> { rowRenderer.ListRow(state.Row, layout, theme) };
        if (state.Row.Todo!.Tags.Length > 0)
        {
            lines.Add(rowRenderer.TagsRow(state.Row, layout, theme));
        }

        return lines;
    }
}
