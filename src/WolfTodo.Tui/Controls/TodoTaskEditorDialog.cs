using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Controls;

public static class TodoTaskEditorDialog
{
    public static int Measure(TodoTaskEditorDialogView view, TuiComponentConstraints constraints) =>
        view.Lines.Count + 2 + (view.TextBoxes ?? []).Sum(textBox => TextBox.Default.Measure(textBox, constraints));

    public static TodoTaskEditorDialogView Create(
        TodoTaskEditorState editor,
        TuiKeyBindings bindings,
        int terminalWidth,
        int terminalHeight)
    {
        var width = Math.Max(1, terminalWidth - 4);
        return CreateModeView(editor, bindings, width) ?? CreateFormView(editor, bindings, width, terminalHeight);
    }

    private static TodoTaskEditorDialogView? CreateModeView(
        TodoTaskEditorState editor,
        TuiKeyBindings bindings,
        int width)
    {
        if (editor.IsEditingContent)
        {
            return new([new("EDITING CONTENT", TodoTaskEditorDialogRole.Hint)]);
        }

        if (editor.IsChoosingProject)
        {
            return MessageView(
                $"{Key(bindings.MoveDown)}/{Key(bindings.MoveUp)} MOVE  " +
                $"{Key(bindings.Open)} SELECT  {Key(bindings.Back)} CANCEL",
                width,
                TodoTaskEditorDialogRole.Hint);
        }

        if (editor.SubtaskTextBox is { } subtaskTextBox)
        {
            var lines = MessageLines("EDITING SUBTASK", width, TodoTaskEditorDialogRole.Hint)
                .Concat(MessageLines(FormMessage(editor, bindings, width), width, editor.Error));
            return new(lines.ToArray(), [subtaskTextBox], Math.Max(3, width));
        }

        if (editor.Mode == TodoTaskEditorMode.ConfirmRemoval)
        {
            var selected = editor.Subtasks[editor.SelectedSubtaskIndex];
            return Warning(
                $"REMOVE '{selected.Title}' AND {selected.DescendantCount} NESTED ITEM(S)?  " +
                $"{Key(bindings.Open)} CONFIRM  {Key(bindings.Back)} CANCEL",
                width);
        }

        return null;
    }

    private static TodoTaskEditorDialogView CreateFormView(
        TodoTaskEditorState editor,
        TuiKeyBindings bindings,
        int width,
        int terminalHeight)
    {
        var textBoxes = VisibleTextBoxes(editor, terminalHeight);
        var rows = FormRows(editor, width);
        var lines = new List<TodoTaskEditorDialogLine> { FormHeading(editor, width) };
        lines.AddRange(VisibleRows(
            rows,
            editor.SelectedIndex,
            terminalHeight - textBoxes.Count * TextBox.Height - 6));
        lines.AddRange(MessageLines(FormMessage(editor, bindings, width), width, editor.Error));
        return new(lines, textBoxes, Math.Max(3, width));
    }

    private static TodoTaskEditorDialogView MessageView(string message, int width, TodoTaskEditorDialogRole role) =>
        new(MessageLines(message, width, role).ToArray());

    private static TodoTaskEditorDialogView Warning(string message, int width) =>
        new(MessageLines(message, width, TodoTaskEditorDialogRole.Warning).ToArray());

    private static IReadOnlyList<(int? Selection, TodoTaskEditorDialogLine Line)> FormRows(
        TodoTaskEditorState editor,
        int width)
    {
        var rows = new List<(int? Selection, TodoTaskEditorDialogLine Line)>();
        if (!editor.IsFieldSelected)
        {
            foreach (var field in Enum.GetValues<TodoFormField>())
            {
                var value = DisplayValue(editor, field);
                rows.Add(((int)field, new(
                    $"  {FieldLabel(field)}: {Truncate(value, Math.Max(1, width - FieldLabel(field).Length - 4))}",
                    TodoTaskEditorDialogRole.Value)));
            }
        }

        rows.Add((TodoTaskEditorState.ContentIndex, new(ContentLabel(editor), TodoTaskEditorDialogRole.Label)));

        var contentLines = editor.Content.Length == 0 ? ["    — No content"] : editor.Content.Split('\n');
        foreach (var line in contentLines.Take(3))
        {
            rows.Add((null, new("    " + Truncate(line, Math.Max(1, width - 4)),
                TodoTaskEditorDialogRole.Value)));
        }

        rows.Add((null, new("  SUBTASKS", TodoTaskEditorDialogRole.Label)));
        if (editor.Subtasks.Length == 0)
        {
            rows.Add((null, new("    — No subtasks", TodoTaskEditorDialogRole.Placeholder)));
            return rows;
        }

        for (var index = 0; index < editor.Subtasks.Length; index++)
        {
            var subtask = editor.Subtasks[index];
            var selection = TodoTaskEditorState.ContentIndex + 1 + index;
            var selected = selection == editor.SelectedIndex;
            var marker = selected ? ">" : " ";
            var icon = subtask.IsCompleted ? "✓" : "◯";
            var suffix = subtask.DescendantCount > 0 ? $"  +{subtask.DescendantCount} nested" : string.Empty;
            var branch = index == editor.Subtasks.Length - 1 ? "└─" : "├─";
            var prefix = $"{marker} {branch} {icon} - ";
            var value = prefix + Truncate(subtask.Title, Math.Max(1, width - prefix.Length - suffix.Length)) + suffix;
            rows.Add((selection, new(value, selected
                ? TodoTaskEditorDialogRole.ActiveValue
                : TodoTaskEditorDialogRole.Value)));
        }

        return rows;
    }

    private static string ContentLabel(TodoTaskEditorState editor) =>
        editor.IsContentSelected ? "> CONTENT" : "  CONTENT";

    private static IReadOnlyList<TextBoxState> VisibleTextBoxes(TodoTaskEditorState editor, int terminalHeight)
    {
        if (!editor.IsFieldSelected) return [];

        var fields = FieldTextBoxes(editor);
        var maxVisible = Math.Clamp((terminalHeight - 8) / TextBox.Height, 1, fields.Count);
        var start = Math.Clamp(editor.SelectedIndex - maxVisible + 1, 0, fields.Count - maxVisible);
        return fields.Skip(start).Take(maxVisible).ToArray();
    }

    private static IReadOnlyList<TextBoxState> FieldTextBoxes(TodoTaskEditorState editor) =>
        Enum.GetValues<TodoFormField>().Select(field =>
        {
            if (editor.FieldTextBox is { } active && field == editor.SelectedField) return active;
            return TextBox.Create(FieldLabel(field), false, DisplayValue(editor, field), field == editor.SelectedField);
        }).ToArray();

    private static string DisplayValue(TodoTaskEditorState editor, TodoFormField field)
    {
        var value = FieldValue(editor, field);
        return value.Length == 0 ? "—" : value;
    }

    private static string FieldLabel(TodoFormField field) => field switch
    {
        TodoFormField.Title => "Title",
        TodoFormField.Reference => "Reference",
        TodoFormField.Priority => "Priority",
        TodoFormField.Tags => "Tags",
        TodoFormField.ScheduledDate => "Scheduled date (YYYY-MM-DD, t+1, w+1, mon)",
        TodoFormField.ScheduledTime => "Scheduled time",
        TodoFormField.Duration => "Duration",
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    private static string FieldValue(TodoTaskEditorState editor, TodoFormField field) => field switch
    {
        TodoFormField.Title => editor.Values.Title,
        TodoFormField.Reference => editor.Values.ExternalReference ?? string.Empty,
        TodoFormField.Priority => editor.Values.Priority?.ToString() ?? string.Empty,
        TodoFormField.Tags => string.Join(' ', editor.Values.Tags.Select(tag => $"#{tag}")),
        TodoFormField.ScheduledDate => editor.ScheduledDate,
        TodoFormField.ScheduledTime => editor.ScheduledTime,
        TodoFormField.Duration => editor.Duration,
        _ => string.Empty
    };

    private static TodoTaskEditorDialogLine FormHeading(TodoTaskEditorState editor, int width) =>
        new(Truncate($"{(editor.IsCreate ? "CREATE" : "EDIT")} TASK // " +
            (editor.Values.Title.Length == 0 ? "NEW TODO" : editor.Values.Title), width),
            TodoTaskEditorDialogRole.Label);

    private static IEnumerable<TodoTaskEditorDialogLine> VisibleRows(
        IReadOnlyList<(int? Selection, TodoTaskEditorDialogLine Line)> rows,
        int selectedIndex,
        int maximumRows)
    {
        var selectedRow = Math.Max(0, rows.ToList().FindIndex(row => row.Selection == selectedIndex));
        var visibleRows = Math.Max(1, maximumRows);
        var start = Math.Clamp(selectedRow - visibleRows + 1, 0, Math.Max(0, rows.Count - visibleRows));
        return rows.Skip(start).Take(visibleRows).Select(row => row.Line);
    }

    private static string FormMessage(TodoTaskEditorState editor, TuiKeyBindings bindings, int width) =>
        editor.Error ?? (editor.FieldTextBox is not null || editor.SubtaskTextBox is not null
            ? "Enter ACCEPT  Esc CANCEL"
            : width <= 66
                ? $"{Key(bindings.Open)} EDIT  {Key(bindings.CreateTodo)} ADD SUBTASK  " +
                  $"{Key(bindings.RemoveContent)} REMOVE  {Key(bindings.SaveForm)} SAVE  {Key(bindings.Back)} CANCEL"
                : $"{Key(bindings.MoveDown)}/{Key(bindings.MoveUp)} MOVE  " +
                  $"{Key(bindings.Open)} EDIT  {Key(bindings.CreateTodo)} ADD SUBTASK  " +
                  $"{Key(bindings.RemoveContent)} REMOVE  Space TOGGLE  " +
                  $"{Key(bindings.SaveForm)} SAVE  {Key(bindings.Back)} CANCEL");

    private static IEnumerable<TodoTaskEditorDialogLine> MessageLines(string message, int width, string? error = null) =>
        MessageLines(message, width, error is null ? TodoTaskEditorDialogRole.Hint : TodoTaskEditorDialogRole.Error);

    private static IEnumerable<TodoTaskEditorDialogLine> MessageLines(
        string message,
        int width,
        TodoTaskEditorDialogRole role) =>
        Wrap(message, width).Select(line => new TodoTaskEditorDialogLine(line, role));

    public static IRenderable CreateRenderable(TodoTaskEditorDialogView view, TuiTheme theme)
    {
        var rows = view.Lines.Select(line => (IRenderable)new Text(line.Text, Style(line.Role, theme))).ToList();
        rows.InsertRange(Math.Min(1, rows.Count), (view.TextBoxes ?? []).Select(textBox => TextBox.Default.Render(
            textBox, theme, new TuiComponentConstraints(view.TextBoxWidth, TextBox.Height))));
        return new Panel(new Rows(rows))
        {
            Border = BoxBorder.Square,
            BorderStyle = new Style(theme.BorderActive),
            Expand = true
        };
    }

    private static Style Style(TodoTaskEditorDialogRole role, TuiTheme theme) => role switch
    {
        TodoTaskEditorDialogRole.Label => new(theme.Heading, decoration: Decoration.Bold),
        TodoTaskEditorDialogRole.Value => new(theme.SecondaryText),
        TodoTaskEditorDialogRole.ActiveValue => new(theme.AccentBright, decoration: Decoration.Bold),
        TodoTaskEditorDialogRole.Placeholder => new(theme.Muted, decoration: Decoration.Dim),
        TodoTaskEditorDialogRole.Hint => new(theme.Muted, decoration: Decoration.Dim),
        TodoTaskEditorDialogRole.Error => new(theme.Error, decoration: Decoration.Bold),
        TodoTaskEditorDialogRole.Warning => new(theme.Warning, decoration: Decoration.Bold),
        _ => new(theme.Text)
    };

    private static string Key(System.Collections.Immutable.ImmutableArray<KeyGesture> gestures) =>
        TuiKeyBindings.ShortestDisplayName(gestures);

    private static IReadOnlyList<string> Wrap(string value, int width)
    {
        var lines = new List<string>();
        var remaining = value;
        while (remaining.Length > width)
        {
            var breakAt = remaining.LastIndexOf(' ', width - 1, width);
            if (breakAt <= 0) breakAt = width;
            lines.Add(remaining[..breakAt].TrimEnd());
            remaining = remaining[breakAt..].TrimStart();
        }
        lines.Add(remaining);
        return lines;
    }

    private static string Truncate(string value, int width) =>
        value.Length <= width ? value : width <= 1 ? value[..width] : value[..(width - 1)] + "…";
}
