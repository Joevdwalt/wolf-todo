using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.ApplicationShell.Rendering;

public sealed class FocusedTaskRenderer(
    Func<int> widthProvider,
    Func<int> heightProvider)
{
    private const int HeaderAndCardChromeRows = 4;
    private readonly SurfaceThemeRenderer themeRenderer = new();
    private readonly StatusRenderer statusRenderer = new();

    public void ShowFocusedTask(FocusedTaskView view, TuiKeyBindings bindings, TuiTheme theme)
    {
        var width = widthProvider();
        var height = heightProvider();
        var editorDialog = view.State.Editor is null
            ? null
            : TodoTaskEditorDialog.Create(view.State.Editor, bindings, width, height);
        var statusLines = StatusLines(view, bindings);
        var overlayHeight = OverlayHeight(view, width, height, bindings);
        var statusHeight = editorDialog?.Height ?? statusLines.Count + 2;
        var available = Math.Max(1, height - statusHeight - overlayHeight - HeaderAndCardChromeRows);

        WriteHeader(theme, width);
        WriteCard(view, theme, width, available);
        WriteOverlay(view, bindings, theme, width, height);

        if (editorDialog is not null)
        {
            themeRenderer.WriteSurface(TodoTaskEditorDialog.CreateRenderable(editorDialog, theme), theme.Surface2, true);
            return;
        }

        var active = view.GlobalCommand is not null || view.CommandPalette is not null;
        var style = view.GlobalError is not null || view.State.Error is not null
            ? themeRenderer.Style(theme.Error, Decoration.Bold)
            : view.State.StatusMessage is not null
                ? themeRenderer.Style(theme.Success, Decoration.Bold)
                : active
                    ? themeRenderer.Style(theme.Accent)
                    : themeRenderer.Style(theme.SecondaryText);
        statusRenderer.WriteStatusPanel(statusLines, theme, style, active);
    }

    private void WriteHeader(TuiTheme theme, int width)
    {
        var text = new Text(
            width < 40 ? "FOCUS" : "WOLF TODO // FOCUS",
            themeRenderer.Style(theme.Accent, Decoration.Bold));
        themeRenderer.WriteSurface(new Align(text, HorizontalAlignment.Center), theme.Background, true);
        AnsiConsole.WriteLine();
    }

    private void WriteCard(FocusedTaskView view, TuiTheme theme, int width, int availableRows)
    {
        var lines = CardLines(view, theme);
        var selectedLine = lines.FindIndex(line => line.ItemIdentity == view.State.SelectedIdentity);
        var visible = FitLines(
            lines.Select(line => line.Renderable).ToArray(),
            availableRows,
            Math.Max(0, selectedLine));
        var cardWidth = Math.Max(1, Math.Min(width - 4, 100));
        var panel = new Panel(new Rows(visible))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = themeRenderer.Style(theme.BorderActive),
            Header = new PanelHeader(view.ProjectTitle.ToUpperInvariant()),
            Padding = new Padding(1, 0),
            Width = cardWidth
        };
        themeRenderer.WriteSurface(
            new Align(themeRenderer.OnSurface(panel, theme.Surface, true), HorizontalAlignment.Center),
            theme.Background,
            true);
    }

    private List<CardLine> CardLines(FocusedTaskView view, TuiTheme theme)
    {
        var lines = new List<CardLine>();
        var root = view.Items[0];
        lines.Add(ItemLine(root, theme, title: true));

        var metadata = Metadata(root.Todo);
        if (metadata.Length > 0)
        {
            lines.Add(new CardLine(new Text(metadata, themeRenderer.Style(theme.SecondaryText))));
        }

        if (root.Todo.Notes.Length > 0)
        {
            lines.Add(new CardLine(new Text(string.Empty)));
            lines.Add(new CardLine(new Text("NOTES", themeRenderer.Style(theme.Heading, Decoration.Bold))));
            lines.AddRange(root.Todo.Notes.Select(note =>
                new CardLine(new Text(note.Text, themeRenderer.Style(theme.Text)))));
        }

        if (view.Items.Length > 1)
        {
            lines.Add(new CardLine(new Text(string.Empty)));
            lines.Add(new CardLine(new Text("CHECKLIST", themeRenderer.Style(theme.Heading, Decoration.Bold))));
            lines.AddRange(view.Items.Skip(1).Select(item => ItemLine(item, theme, title: false)));
        }

        if (root.Todo.Notes.Length == 0 && view.Items.Length == 1)
        {
            lines.Add(new CardLine(new Text(string.Empty)));
            lines.Add(new CardLine(new Text("NO ADDITIONAL DETAILS", themeRenderer.Style(theme.Muted, Decoration.Dim))));
        }

        return lines;
    }

    private CardLine ItemLine(FocusedTaskItem item, TuiTheme theme, bool title)
    {
        var prefix = item.IsSelected ? "> " : "  ";
        var tree = title ? string.Empty : TodoTreeFormatter.Format(item.TreePath);
        var glyph = item.Todo.IsCompleted ? "✓" : "◯";
        var style = item.IsSelected
            ? themeRenderer.Style(theme.AccentBright, Decoration.Bold)
            : item.Todo.IsCompleted
                ? themeRenderer.Style(theme.Muted, Decoration.Dim)
                : themeRenderer.Style(title ? theme.Heading : theme.Text, title ? Decoration.Bold : Decoration.None);
        return new CardLine(new Text($"{prefix}{tree}{glyph} {item.Todo.Title}", style), item.Identity);
    }

    private static string Metadata(TodoItem todo)
    {
        var values = new List<string>();
        if (todo.Tags.Length > 0) values.Add(string.Join(' ', todo.Tags.Select(tag => $"#{tag}")));
        if (todo.Priority is not null) values.Add(todo.Priority.ToString()!.ToUpperInvariant());
        if (todo.Schedule is not null)
        {
            values.Add(todo.Schedule.Time is { } time
                ? $"{todo.Schedule.Date:yyyy-MM-dd} {time:HH:mm}"
                : $"{todo.Schedule.Date:yyyy-MM-dd}");
        }
        if (todo.Duration is { } duration) values.Add($"◷ {(int)duration.TotalMinutes}m");
        if (!string.IsNullOrWhiteSpace(todo.ExternalReference)) values.Add(todo.ExternalReference);
        return string.Join("  ", values);
    }

    private IReadOnlyList<BrowserStatusLine> StatusLines(FocusedTaskView view, TuiKeyBindings bindings)
    {
        if (view.CommandPalette is not null)
        {
            return statusRenderer.DefaultStatusLines([statusRenderer.CommandPaletteFooter(bindings)]);
        }

        if (view.GlobalCommand is not null) return [new BrowserStatusLine(view.GlobalCommand)];
        if (view.GlobalError is not null) return [new BrowserStatusLine(view.GlobalError)];
        if (view.State.Error is not null) return [new BrowserStatusLine(view.State.Error)];
        if (view.State.StatusMessage is not null) return [new BrowserStatusLine(view.State.StatusMessage)];
        if (view.ReloadStatus is not null) return [new BrowserStatusLine(view.ReloadStatus.Message)];

        var normal =
            $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} ITEM  " +
            $"{Shortest(bindings.EditTodoContent)} EDIT  {Shortest(bindings.ToggleTodo)} COMPLETE  " +
            $"{Shortest(bindings.ToggleTimer)} TIMER  {Shortest(bindings.StartPomodoro)} POMODORO  " +
            $"{Shortest(bindings.CommandMode)} COMMAND  {Shortest(bindings.CommandPalette)} ACTIONS  " +
            $"{Shortest(bindings.Back)} EXIT";
        var lines = statusRenderer.DefaultStatusLines(statusRenderer.Wrap(normal, Math.Max(1, widthProvider() - 4)));
        if (view.TimerStatus is not null)
        {
            return
            [
                new BrowserStatusLine(
                    view.TimerStatus,
                    view.TimerIsBright ? BrowserStatusRole.TimerActive : BrowserStatusRole.TimerInactive),
                .. lines
            ];
        }

        return view.PomodoroCompletion is null
            ? lines
            : [new BrowserStatusLine(view.PomodoroCompletion.Status, BrowserStatusRole.PomodoroComplete), .. lines];
    }

    private static int OverlayHeight(
        FocusedTaskView view,
        int width,
        int height,
        TuiKeyBindings bindings)
    {
        if (view.PomodoroPrompt is not null) return PomodoroPromptRenderer.Height;
        if (view.CommandPalette is not null)
        {
            var rows = TerminalLayout.SelectListRows(height);
            return SelectList.Default.Measure(
                CommandPaletteSelectList(view.CommandPalette, bindings),
                new TuiComponentConstraints(width, rows));
        }

        return view.State.Editor?.ContentTextBox is { } textBox
            ? MultilineTextBox.Default.Measure(
                textBox,
                new TuiComponentConstraints(width, TerminalLayout.TextBoxRows(height)))
            : 0;
    }

    private static void WriteOverlay(
        FocusedTaskView view,
        TuiKeyBindings bindings,
        TuiTheme theme,
        int width,
        int height)
    {
        if (view.PomodoroPrompt is not null)
        {
            AnsiConsole.Write(PomodoroPromptRenderer.Render(view.PomodoroPrompt, theme, width));
        }
        else if (view.CommandPalette is not null)
        {
            AnsiConsole.Write(SelectList.Default.Render(
                CommandPaletteSelectList(view.CommandPalette, bindings),
                theme,
                new TuiComponentConstraints(width, TerminalLayout.SelectListRows(height))));
        }
        else if (view.State.Editor?.ContentTextBox is { } textBox)
        {
            AnsiConsole.Write(MultilineTextBox.Default.Render(
                textBox,
                theme,
                new TuiComponentConstraints(width, TerminalLayout.TextBoxRows(height)),
                TuiKeyBindings.ShortestDisplayName(bindings.SaveForm)));
        }
    }

    private static string Shortest(System.Collections.Immutable.ImmutableArray<KeyGesture> gestures) =>
        TuiKeyBindings.ShortestDisplayName(gestures);

    private static IReadOnlyList<IRenderable> FitLines(
        IReadOnlyList<IRenderable> lines,
        int contentHeight,
        int selectedIndex)
    {
        if (lines.Count <= contentHeight) return lines;
        var start = Math.Clamp(selectedIndex - contentHeight + 1, 0, lines.Count - contentHeight);
        return lines.Skip(start).Take(contentHeight).ToArray();
    }

    private static SelectListView CommandPaletteSelectList(
        CommandPaletteView palette,
        TuiKeyBindings bindings) => new(
        "Command palette",
        palette.Items.Select(item => new SelectOption(
            $"{item.Group}: {item.Label}",
            $"[{item.Binding}]" + (item.IsEnabled ? string.Empty : $" — {item.DisabledReason}"),
            item.IsEnabled)).ToArray(),
        palette.SelectedIndex,
        palette.State.IsSearching ? palette.State.Query : null,
        "No matching actions",
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
        $"{Shortest(bindings.FilterMode)} SEARCH  {Shortest(bindings.Open)} RUN  " +
        $"{Shortest(bindings.Back)} CLOSE",
        palette.State.Error);

    private sealed record CardLine(IRenderable Renderable, TodoIdentity? ItemIdentity = null);
}
