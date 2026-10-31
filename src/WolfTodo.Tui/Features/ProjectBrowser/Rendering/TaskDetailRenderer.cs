using System.Collections.Immutable;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.ProjectBrowser.Rendering;

public sealed class TaskDetailRenderer
{
    private readonly SurfaceThemeRenderer themeRenderer = new();
    private readonly TodoRowRenderer rows = new();

    public void AddTitle(List<IRenderable> lines, TodoItem todo, TuiTheme theme) =>
        lines.Add(new Text(todo.Title, themeRenderer.Style(theme.Heading, Decoration.Bold)));

    public void AddIdentity(List<IRenderable> lines, string? projectTitle, TodoIdentity? identity, TuiTheme theme)
    {
        AddField(lines, "Project", projectTitle, theme, theme.Text);
        if (identity is not null)
        {
            AddField(lines, "Link", TaskLinkCode.Generate(identity.ProjectPath, identity.SourceLine), theme, theme.Info);
        }
    }

    public void AddMetadata(List<IRenderable> lines, TodoItem todo, TuiTheme theme, bool includeSection = true, bool includeSchedule = true)
    {
        if (includeSection)
        {
            AddField(lines, "Section", todo.SectionPath, theme, theme.Text);
        }

        AddField(lines, "Reference", todo.ExternalReference, theme, theme.Info);
        AddField(lines, "Priority", todo.Priority?.ToString(), theme, rows.PriorityColor(todo.Priority, theme));
        AddField(lines, "Tags", todo.Tags.Length == 0 ? null : rows.FormatTags(todo.Tags, ", "), theme, theme.Tag);
        if (includeSchedule)
        {
            AddField(lines, "Scheduled", todo.Schedule is null ? null : rows.FormatSchedule(todo.Schedule), theme, theme.Date);
        }
        AddField(lines, "Duration", rows.FormatDuration(todo.Duration), theme, theme.Info);
    }

    public void AddContent(List<IRenderable> lines, TodoItem todo, TuiTheme theme, bool recursiveSubtasks, bool dimEmpty)
    {
        if (todo.Notes.Length > 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("NOTES", themeRenderer.Style(theme.Heading, Decoration.Bold)));
            lines.AddRange(todo.Notes.Select(note => new Text($"{TodoGlyphs.NoteBullet} {note.Text}", themeRenderer.Style(theme.Text))));
        }
        if (todo.Subtasks.Length > 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("SUBTASKS", themeRenderer.Style(theme.Heading, Decoration.Bold)));
            if (recursiveSubtasks)
            {
                lines.AddRange(FlattenSubtasks(todo.Subtasks)
                    .Select(item => rows.DetailLine(item.Todo, item.TreePath, false, theme)));
            }
            else
            {
                lines.AddRange(todo.Subtasks.Select(subtask => rows.DetailLine(subtask, [], false, theme)));
            }
        }
        if (todo.Notes.Length == 0 && todo.Subtasks.Length == 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("NO ADDITIONAL DETAILS", themeRenderer.Style(theme.Muted, dimEmpty ? Decoration.Dim : Decoration.None)));
        }
    }

    public static IEnumerable<(TodoItem Todo, ImmutableArray<TodoTreeSegment> TreePath)> FlattenSubtasks(
        ImmutableArray<TodoItem> todos, ImmutableArray<TodoTreeSegment> parentPath = default)
    {
        for (var index = 0; index < todos.Length; index++)
        {
            var path = (parentPath.IsDefault ? ImmutableArray<TodoTreeSegment>.Empty : parentPath).Add(
                index == todos.Length - 1 ? TodoTreeSegment.LastSibling : TodoTreeSegment.HasFollowingSibling);
            var todo = todos[index];
            yield return (todo, path);
            foreach (var descendant in FlattenSubtasks(todo.Subtasks, path))
            {
                yield return descendant;
            }
        }
    }

    public void AddField(List<IRenderable> lines, string name, string? value, TuiTheme theme, Color valueColor)
    {
        if (string.IsNullOrEmpty(value)) return;
        var line = new System.Text.StringBuilder();
        themeRenderer.AppendStyled(line, $"{name.ToUpperInvariant()}: ", theme.Heading, Decoration.Bold);
        themeRenderer.AppendStyled(line, value, valueColor);
        lines.Add(new Markup(line.ToString()));
    }
}
