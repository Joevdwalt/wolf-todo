using System.Collections.Immutable;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.ProjectBrowser.Rendering;

public sealed class TodoRowRenderer
{
    private const string OpenTodoGlyph = "◯";
    private const string CompletedTodoGlyph = "✓";
    private static readonly SurfaceThemeRenderer ThemeRenderer = new();

    public TodoColumnLayout Columns(int contentWidth, bool includeProject) =>
        new(
            contentWidth,
            Math.Max(1, contentWidth - (6 +
                (includeProject && contentWidth >= 52 ? 12 : 0) +
                (contentWidth >= 44 ? 18 : 0))),
            includeProject && contentWidth >= 52,
            10,
            contentWidth >= 44,
            16);

    public IRenderable ColumnHeader(TodoColumnLayout layout, TuiTheme theme) =>
        new Text(
            Truncate(
                $"  S P {FitColumn("TASK", layout.TaskWidth)}" +
                (layout.ShowProject ? $"  {FitColumn("PROJECT", layout.ProjectWidth)}" : string.Empty) +
                (layout.ShowSchedule ? $"  {FitColumn("SCHEDULED", layout.ScheduleWidth)}" : string.Empty),
                layout.ContentWidth),
            ThemeRenderer.Style(theme.Heading, Decoration.Bold));

    public IRenderable ListRow(TodoRow row, TodoColumnLayout layout, TuiTheme theme)
    {
        var todo = row.Todo!;
        var cursor = row.IsSelected ? ">" : " ";
        var treePrefix = TodoTreeFormatter.Format(row.TreePath);
        var status = StatusGlyph(todo.IsCompleted);
        var priority = PriorityCode(todo.Priority);
        var prefixWidth = DisplayWidth(treePrefix);
        var visiblePrefix = prefixWidth >= layout.TaskWidth
            ? FitColumn(treePrefix, layout.TaskWidth)
            : treePrefix;
        var title = prefixWidth >= layout.TaskWidth
            ? string.Empty
            : FitColumn(todo.Title, layout.TaskWidth - prefixWidth);
        var selectedColor = row.IsSelected ? theme.AccentBright : theme.Text;
        var baseColor = todo.IsCompleted ? theme.Muted : selectedColor;
        var treeColor = row.IsSelected ? theme.AccentBright : theme.Muted;
        var decoration = row.IsSelected
            ? Decoration.Bold
            : todo.IsCompleted ? Decoration.Dim : Decoration.None;
        var line = new System.Text.StringBuilder();
        ThemeRenderer.AppendStyled(line, $"{cursor} ", baseColor, decoration);
        ThemeRenderer.AppendStyled(line, status, baseColor, decoration);
        ThemeRenderer.AppendStyled(line, " ", baseColor, decoration);
        ThemeRenderer.AppendStyled(
            line,
            priority,
            row.IsSelected || todo.IsCompleted ? baseColor : PriorityColor(todo.Priority, theme),
            decoration);
        ThemeRenderer.AppendStyled(line, " ", baseColor, decoration);
        ThemeRenderer.AppendStyled(line, visiblePrefix, treeColor, decoration);
        ThemeRenderer.AppendStyled(line, title, baseColor, decoration);
        if (layout.ShowProject)
        {
            ThemeRenderer.AppendStyled(
                line,
                $"  {FitColumn(row.ProjectTitle ?? "-", layout.ProjectWidth)}",
                baseColor,
                decoration);
        }

        if (layout.ShowSchedule)
        {
            var schedule = todo.Schedule is null ? "-" : FormatSchedule(todo.Schedule);
            var scheduleColor = row.IsSelected || todo.IsCompleted ? baseColor : theme.Date;
            ThemeRenderer.AppendStyled(
                line,
                $"  {FitColumn(schedule, layout.ScheduleWidth)}",
                scheduleColor,
                decoration);
        }

        var content = (IRenderable)new Markup(line.ToString());
        return row.IsSelected ? ThemeRenderer.OnSurface(content, theme.Surface2, true) : content;
    }

    public IRenderable TagsRow(TodoRow row, TodoColumnLayout layout, TuiTheme theme)
    {
        var todo = row.Todo!;
        var treeContinuation = TodoTreeFormatter.FormatContinuation(row.TreePath);
        var treeWidth = DisplayWidth(treeContinuation);
        var visibleTreeWidth = Math.Min(treeWidth, Math.Max(0, layout.TaskWidth - 1));
        var visibleTree = FitColumn(treeContinuation, visibleTreeWidth);
        var tagWidth = layout.TaskWidth - visibleTreeWidth;
        var tags = string.Join(' ', todo.Tags.Select(tag => $"#{tag}"));
        var tagColor = row.IsSelected ? theme.AccentBright : todo.IsCompleted ? theme.Muted : theme.Tag;
        var treeColor = row.IsSelected ? theme.AccentBright : theme.Muted;
        var decoration = row.IsSelected
            ? Decoration.Bold
            : todo.IsCompleted ? Decoration.Dim : Decoration.None;
        var line = new System.Text.StringBuilder();
        ThemeRenderer.AppendStyled(line, new string(' ', 6), tagColor, decoration);
        ThemeRenderer.AppendStyled(line, visibleTree, treeColor, decoration);
        ThemeRenderer.AppendStyled(line, FitColumn(tags, tagWidth), tagColor, decoration);

        var content = (IRenderable)new Markup(line.ToString());
        return row.IsSelected ? ThemeRenderer.OnSurface(content, theme.Surface2, true) : content;
    }

    public IRenderable DetailLine(
        TodoItem todo,
        ImmutableArray<TodoTreeSegment> treePath,
        bool selected,
        TuiTheme theme)
    {
        var cursor = selected ? ">" : " ";
        var treePrefix = TodoTreeFormatter.Format(treePath);
        var status = StatusGlyph(todo.IsCompleted);
        var reference = todo.ExternalReference is null ? string.Empty : $"{todo.ExternalReference} - ";
        var priority = PriorityCode(todo.Priority);
        var tags = todo.Tags.Length == 0 ? string.Empty : $" {string.Join(' ', todo.Tags.Select(tag => $"#{tag}"))}";
        var schedule = todo.Schedule is null ? string.Empty : $" ⏳ {FormatSchedule(todo.Schedule)}";

        var line = new System.Text.StringBuilder();
        ThemeRenderer.AppendStyled(
            line,
            cursor,
            selected ? theme.Accent : todo.IsCompleted ? theme.Muted : theme.Text,
            selected ? Decoration.Bold : todo.IsCompleted ? Decoration.Dim : Decoration.None);
        ThemeRenderer.AppendStyled(
            line,
            $" {treePrefix}",
            selected ? theme.Accent : theme.Muted,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        ThemeRenderer.AppendStyled(
            line,
            status,
            todo.IsCompleted ? theme.Muted : theme.Accent,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        ThemeRenderer.AppendStyled(line, $" {priority}", todo.IsCompleted ? theme.Muted : PriorityColor(todo.Priority, theme));
        ThemeRenderer.AppendStyled(
            line,
            $" {reference}{todo.Title}",
            todo.IsCompleted ? theme.Muted : theme.Text,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        ThemeRenderer.AppendStyled(
            line,
            tags,
            todo.IsCompleted ? theme.Muted : theme.Tag,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        ThemeRenderer.AppendStyled(
            line,
            schedule,
            todo.IsCompleted ? theme.Muted : theme.Date,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        return new Markup(line.ToString());
    }

    public string FormatSchedule(TodoSchedule schedule) =>
        schedule.Time is null
            ? schedule.Date.ToString("yyyy-MM-dd")
            : $"{schedule.Date:yyyy-MM-dd} {schedule.Time:HH:mm}";

    public string? FormatDuration(TimeSpan? duration) =>
        duration is null ? null : $"{(int)duration.Value.TotalMinutes}m";

    public string PriorityCode(TodoPriority? priority) => priority switch
    {
        TodoPriority.Highest => "!",
        TodoPriority.High => "H",
        TodoPriority.Medium => "M",
        TodoPriority.Low => "L",
        TodoPriority.Lowest => ".",
        _ => "-"
    };

    public string StatusGlyph(bool isCompleted) =>
        isCompleted ? CompletedTodoGlyph : OpenTodoGlyph;

    public string FitColumn(string value, int width)
    {
        var result = Truncate(value, width);
        return result + new string(' ', Math.Max(0, width - DisplayWidth(result)));
    }

    public string Truncate(string value, int width)
    {
        if (DisplayWidth(value) <= width)
        {
            return value;
        }

        var result = new System.Text.StringBuilder();
        var remainingWidth = Math.Max(0, width - 1);

        foreach (var rune in value.EnumerateRunes())
        {
            var runeWidth = rune.ToString().GetCellWidth();
            if (runeWidth > remainingWidth)
            {
                break;
            }

            result.Append(rune.ToString());
            remainingWidth -= runeWidth;
        }

        return result.Append('…').ToString();
    }

    public int DisplayWidth(string value) =>
        value.GetCellWidth();

    public IEnumerable<(TodoItem Todo, ImmutableArray<TodoTreeSegment> TreePath)> FlattenSubtasks(
        ImmutableArray<TodoItem> todos,
        ImmutableArray<TodoTreeSegment> parentPath = default)
    {
        for (var index = 0; index < todos.Length; index++)
        {
            var path = (parentPath.IsDefault ? ImmutableArray<TodoTreeSegment>.Empty : parentPath).Add(
                index == todos.Length - 1
                    ? TodoTreeSegment.LastSibling
                    : TodoTreeSegment.HasFollowingSibling);
            var todo = todos[index];
            yield return (todo, path);

            foreach (var descendant in FlattenSubtasks(todo.Subtasks, path))
            {
                yield return descendant;
            }
        }
    }

    public Color PriorityColor(TodoPriority? priority, TuiTheme theme) => priority switch
    {
        TodoPriority.Highest => theme.Error,
        TodoPriority.High => theme.Warning,
        TodoPriority.Medium => theme.Accent,
        TodoPriority.Low => theme.Muted,
        TodoPriority.Lowest => theme.Muted,
        _ => theme.Text
    };
}
