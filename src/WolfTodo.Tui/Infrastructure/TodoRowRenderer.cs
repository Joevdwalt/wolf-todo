using System.Collections.Immutable;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Infrastructure;

public sealed class TodoRowRenderer
{
    public TodoColumnLayout Columns(int contentWidth, bool includeProject) =>
        BrowserRenderer.TodoColumns(contentWidth, includeProject);

    public IRenderable ColumnHeader(TodoColumnLayout layout, TuiTheme theme) =>
        BrowserRenderer.TodoColumnHeader(layout, theme);

    public IRenderable ListRow(TodoRow row, TodoColumnLayout layout, TuiTheme theme) =>
        BrowserRenderer.TodoListRow(row, layout, theme);

    public IRenderable TagsRow(TodoRow row, TodoColumnLayout layout, TuiTheme theme) =>
        BrowserRenderer.TodoTagsRow(row, layout, theme);

    public IRenderable DetailLine(
        TodoItem todo,
        ImmutableArray<TodoTreeSegment> treePath,
        bool selected,
        TuiTheme theme) =>
        BrowserRenderer.DetailedTodoLine(todo, treePath, selected, theme);

    public string FormatSchedule(TodoSchedule schedule) =>
        BrowserRenderer.FormatSchedule(schedule);

    public string? FormatDuration(TimeSpan? duration) =>
        BrowserRenderer.FormatDuration(duration);

    public string PriorityCode(TodoPriority? priority) =>
        BrowserRenderer.PriorityCode(priority);

    public string StatusGlyph(bool isCompleted) =>
        BrowserRenderer.TodoStatusGlyph(isCompleted);

    public string Truncate(string value, int width) =>
        BrowserRenderer.Truncate(value, width);

    public int DisplayWidth(string value) =>
        BrowserRenderer.DisplayWidth(value);

    public IEnumerable<(TodoItem Todo, ImmutableArray<TodoTreeSegment> TreePath)> FlattenSubtasks(
        ImmutableArray<TodoItem> todos,
        ImmutableArray<TodoTreeSegment> parentPath = default) =>
        BrowserRenderer.FlattenSubtasks(todos, parentPath);
}
