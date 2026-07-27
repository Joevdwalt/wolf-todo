using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Controls;

/// <summary>
/// The reusable, terminal-native task editor dialog. It owns the editor's
/// logical rows so production surfaces and the component sandbox cannot drift.
/// </summary>
public static class TodoTaskEditorDialog
{
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

        if (editor.TitleTextBox is not null)
        {
            return MessageView(editor.Error ?? "Enter ACCEPT  Esc CANCEL", width,
                editor.Error is null ? TodoTaskEditorDialogRole.Hint : TodoTaskEditorDialogRole.Error);
        }

        return editor.Mode switch
        {
            _ when editor.IsChoosingProject => ProjectPickerView(bindings, width),
            TodoTaskEditorMode.ChooseContentType => ContentTypePickerView(bindings, width),
            TodoTaskEditorMode.ConfirmRemoval => RemovalConfirmationView(editor, bindings, width),
            TodoTaskEditorMode.Edit when editor.IsAddingContent => AddContentView(editor, width),
            _ => null
        };
    }

    private static TodoTaskEditorDialogView CreateFormView(
        TodoTaskEditorState editor,
        TuiKeyBindings bindings,
        int width,
        int terminalHeight)
    {
        var rows = FormRows(editor, width);
        var lines = new List<TodoTaskEditorDialogLine> { FormHeading(editor, width) };
        lines.AddRange(VisibleRows(rows, editor.SelectedIndex, terminalHeight));
        lines.AddRange(MessageLines(FormMessage(editor, bindings), width, editor.Error));
        return new(lines);
    }

    private static TodoTaskEditorDialogView ProjectPickerView(TuiKeyBindings bindings, int width) =>
        MessageView(
            $"{Key(bindings.MoveDown)}/{Key(bindings.MoveUp)} MOVE  " +
            $"{Key(bindings.Open)} SELECT  {Key(bindings.Back)} CANCEL",
            width,
            TodoTaskEditorDialogRole.Hint);

    private static TodoTaskEditorDialogView ContentTypePickerView(TuiKeyBindings bindings, int width) =>
        Hint(
            $"{Key(bindings.MoveDown)}/{Key(bindings.MoveUp)} MOVE  " +
            $"{Key(bindings.Open)} SELECT  {Key(bindings.Back)} CANCEL",
            width);

    private static TodoTaskEditorDialogView RemovalConfirmationView(
        TodoTaskEditorState editor,
        TuiKeyBindings bindings,
        int width)
    {
        var selected = (ContentSubtaskDraft)editor.Items[editor.SelectedContentIndex];
        return Warning(
            $"REMOVE '{selected.Title}' AND {selected.DescendantCount} NESTED ITEM(S)?  " +
            $"{Key(bindings.Open)} CONFIRM  {Key(bindings.Back)} CANCEL",
            width);
    }

    private static TodoTaskEditorDialogView AddContentView(TodoTaskEditorState editor, int width) =>
        Active($"ADD {editor.AddKind.ToString().ToUpperInvariant()}: {editor.Draft}_  Enter ACCEPT  Esc CANCEL", width);

    private static TodoTaskEditorDialogView MessageView(string message, int width, TodoTaskEditorDialogRole role) =>
        new(MessageLines(message, width, role).ToArray());

    private static IReadOnlyList<(int? Selection, TodoTaskEditorDialogLine Line)> FormRows(
        TodoTaskEditorState editor,
        int width)
    {
        var rows = FieldValues(editor)
            .Select((field, index) => ((int?)index, FieldLine(editor, index, field.Label, field.Value, width)))
            .ToList();
        rows.Add((null, new("  CONTENT", TodoTaskEditorDialogRole.Label)));
        AddContentRows(rows, editor, width);
        return rows;
    }

    private static IReadOnlyList<(string Label, string Value)> FieldValues(TodoTaskEditorState editor) =>
    [
        ("Title", editor.Values.Title),
        ("Reference", editor.Values.ExternalReference ?? string.Empty),
        ("Priority", editor.Values.Priority?.ToString() ?? string.Empty),
        ("Tags", string.Join(' ', editor.Values.Tags.Select(tag => $"#{tag}"))),
        ("Scheduled date (YYYY-MM-DD, t+1, w+1)", editor.ScheduledDate),
        ("Scheduled time", editor.ScheduledTime),
        ("Duration", editor.Duration)
    ];

    private static void AddContentRows(
        ICollection<(int? Selection, TodoTaskEditorDialogLine Line)> rows,
        TodoTaskEditorState editor,
        int width)
    {
        if (editor.Items.Length == 0)
        {
            rows.Add((null, new("    — No notes or subtasks", TodoTaskEditorDialogRole.Placeholder)));
            return;
        }

        for (var index = 0; index < editor.Items.Length; index++)
        {
            var selection = TodoTaskEditorState.FieldCount + index;
            var selected = selection == editor.SelectedIndex;
            var draft = editor.Mode == TodoTaskEditorMode.Edit && selected ? editor.Draft + "_" : null;
            rows.Add((selection, new(
                ContentLine(editor.Items[index], selected, width, draft),
                selected ? TodoTaskEditorDialogRole.ActiveValue : TodoTaskEditorDialogRole.Value)));
        }
    }

    private static TodoTaskEditorDialogLine FormHeading(TodoTaskEditorState editor, int width) =>
        new(Truncate(
            $"{(editor.IsCreate ? "CREATE" : "EDIT")} TASK // " +
            $"{(editor.Values.Title.Length == 0 ? "NEW TODO" : editor.Values.Title)}",
            width), TodoTaskEditorDialogRole.Label);

    private static IEnumerable<TodoTaskEditorDialogLine> VisibleRows(
        IReadOnlyList<(int? Selection, TodoTaskEditorDialogLine Line)> rows,
        int selectedIndex,
        int terminalHeight)
    {
        var selectedRow = Math.Max(0, rows.ToList().FindIndex(row => row.Selection == selectedIndex));
        var visibleRows = Math.Max(1, Math.Min(12, terminalHeight - 12));
        var start = Math.Clamp(selectedRow - visibleRows + 1, 0, Math.Max(0, rows.Count - visibleRows));
        return rows.Skip(start).Take(visibleRows).Select(row => row.Line);
    }

    private static string FormMessage(TodoTaskEditorState editor, TuiKeyBindings bindings) =>
        editor.Error ?? (editor.Mode == TodoTaskEditorMode.Edit
            ? "Enter ACCEPT  Esc CANCEL"
            : $"{Key(bindings.MoveDown)}/{Key(bindings.MoveUp)} MOVE  " +
              $"{Key(bindings.Open)} EDIT  {Key(bindings.CreateTodo)} ADD  " +
              $"{Key(bindings.RemoveContent)} REMOVE  Space TOGGLE  " +
              $"{Key(bindings.SaveForm)} SAVE  {Key(bindings.Back)} CANCEL");

    private static IEnumerable<TodoTaskEditorDialogLine> MessageLines(
        string message,
        int width,
        string? error = null) =>
        MessageLines(message, width, error is null ? TodoTaskEditorDialogRole.Hint : TodoTaskEditorDialogRole.Error);

    private static IEnumerable<TodoTaskEditorDialogLine> MessageLines(
        string message,
        int width,
        TodoTaskEditorDialogRole role) =>
        Wrap(message, width).Select(line => new TodoTaskEditorDialogLine(line, role));

    public static IRenderable CreateRenderable(TodoTaskEditorDialogView view, TuiTheme theme) =>
        new Panel(new Rows(view.Lines.Select(line => new Text(line.Text, Style(line.Role, theme)))) )
        {
            Border = BoxBorder.Square,
            BorderStyle = new Style(theme.BorderActive),
            Expand = true
        };

    private static TodoTaskEditorDialogView Hint(string value, int width) =>
        new(Wrap(value, width).Select(line => new TodoTaskEditorDialogLine(line, TodoTaskEditorDialogRole.Hint)).ToArray());

    private static TodoTaskEditorDialogView Warning(string value, int width) =>
        new(Wrap(value, width).Select(line => new TodoTaskEditorDialogLine(line, TodoTaskEditorDialogRole.Warning)).ToArray());

    private static TodoTaskEditorDialogView Active(string value, int width) =>
        new(Wrap(value, width).Select(line => new TodoTaskEditorDialogLine(line, TodoTaskEditorDialogRole.ActiveValue)).ToArray());

    private static TodoTaskEditorDialogLine FieldLine(
        TodoTaskEditorState editor,
        int index,
        string label,
        string value,
        int width)
    {
        var selected = editor.SelectedIndex == index;
        var labelWidth = Math.Min(16, Math.Max(6, width / 3));
        var prefix = $"{(selected ? ">" : " ")} {FitColumn(label.ToUpperInvariant(), labelWidth)} ";
        var display = editor.Mode == TodoTaskEditorMode.Edit && selected
                ? editor.Draft + "_"
            : string.IsNullOrEmpty(value) ? "—" : value;
        var role = selected
            ? TodoTaskEditorDialogRole.ActiveValue
            : string.IsNullOrEmpty(value)
                ? TodoTaskEditorDialogRole.Placeholder
                : TodoTaskEditorDialogRole.Value;
        return new(prefix + Truncate(display, Math.Max(1, width - prefix.Length)), role);
    }

    private static string ContentLine(ContentItemDraft item, bool selected, int width, string? valueOverride)
    {
        var marker = selected ? ">" : " ";
        var icon = item switch
        {
            ContentNoteDraft => "•",
            ContentSubtaskDraft subtask => subtask.IsCompleted ? "✓" : "◯",
            _ => "-"
        };
        var value = valueOverride ?? item switch
        {
            ContentNoteDraft note => note.Text,
            ContentSubtaskDraft subtask => subtask.Title,
            _ => string.Empty
        };
        var suffix = item is ContentSubtaskDraft { DescendantCount: > 0 } nested
            ? $"  +{nested.DescendantCount} nested"
            : string.Empty;
        var prefix = $"{marker} {icon} ";
        return prefix.Length + suffix.Length >= width
            ? Truncate(prefix + value, width)
            : prefix + Truncate(value, width - prefix.Length - suffix.Length) + suffix;
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
            if (breakAt <= 0)
            {
                breakAt = width;
            }

            lines.Add(remaining[..breakAt].TrimEnd());
            remaining = remaining[breakAt..].TrimStart();
        }

        lines.Add(remaining);
        return lines;
    }

    private static string FitColumn(string value, int width) =>
        value.Length <= width ? value.PadRight(width) : Truncate(value, width);

    private static string Truncate(string value, int width) =>
        value.Length <= width ? value : width <= 1 ? value[..width] : value[..(width - 1)] + "…";
}

public sealed record TodoTaskEditorDialogView(IReadOnlyList<TodoTaskEditorDialogLine> Lines)
{
    public int Height => Lines.Count + 2;
}

public sealed record TodoTaskEditorDialogLine(string Text, TodoTaskEditorDialogRole Role);

public enum TodoTaskEditorDialogRole
{
    Default,
    Label,
    Value,
    ActiveValue,
    Placeholder,
    Hint,
    Error,
    Warning
}
