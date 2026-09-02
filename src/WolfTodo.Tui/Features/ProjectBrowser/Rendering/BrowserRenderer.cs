using System.Collections.Immutable;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Controls;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.ProjectBrowser.Rendering;

public sealed class BrowserRenderer
{
    private const int TodoSelectionLookAheadRows = 10;

    private const string OpenTodoGlyph = "◯";
    private const string CompletedTodoGlyph = "✓";

    private readonly Func<int> widthProvider;
    private readonly Func<int> heightProvider;
    private readonly Func<DateOnly> todayProvider;
    private readonly Func<DateTime> nowProvider;
    private readonly StatusRenderer statusRenderer = new();

    public BrowserRenderer() : this(TerminalLayout.SafeWindowWidth, TerminalLayout.SafeWindowHeight, null, null)
    {
    }

    public BrowserRenderer(
        Func<int> widthProvider,
        Func<int> heightProvider,
        Func<DateOnly>? todayProvider = null,
        Func<DateTime>? nowProvider = null)
    {
        this.widthProvider = widthProvider;
        this.heightProvider = heightProvider;
        this.todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today));
        this.nowProvider = nowProvider ?? (() => DateTime.Now);
    }

    public void ShowBrowser(TabStripView tabs, BrowserView view, TuiKeyBindings keyBindings) =>
        ShowBrowser(tabs, view, keyBindings, TuiThemes.Wolf);

    public void ShowBrowser(
        TabStripView tabs,
        BrowserView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme)
    {
        var context = CreateBrowserRenderContext(view, keyBindings);
        RenderBrowserHeader(tabs, view, keyBindings, theme, context);
        RenderBrowserBody(view, theme, context);
        RenderBrowserOverlay(keyBindings, theme, context);
        statusRenderer.WriteBrowserStatus(context.StatusLines, view, theme, context.EditorDialog);
    }

    public BrowserRenderContext CreateBrowserRenderContext(
        BrowserView view,
        TuiKeyBindings keyBindings)
    {
        var width = widthProvider();
        var height = heightProvider();
        var compact = width < 80 || height < 18;
        var today = todayProvider();
        var selectList = BrowserSelectList(view, keyBindings);
        var selectRows = TerminalLayout.SelectListRows(height);
        var textBox = BrowserTextBox(view);
        var editorDialog = CreateBrowserEditorDialog(view, keyBindings, width, height);
        var textBoxRows = TerminalLayout.TextBoxRows(height);
        var statusLines = statusRenderer.BrowserStatus(view, keyBindings, compact, width, height);
        var contentHeight = BrowserContentHeight(
            height,
            TerminalLayout.DialogContentHeight(editorDialog) ?? statusLines.Count,
            TerminalLayout.PickerHeight(selectList, width, selectRows, textBox, textBoxRows) +
            (view.PomodoroPrompt is null ? 0 : PomodoroPromptRenderer.Height));

        return new BrowserRenderContext(
            width,
            height,
            compact,
            today,
            selectList,
            selectRows,
            textBox,
            textBoxRows,
            view.PomodoroPrompt,
            editorDialog,
            statusLines,
            contentHeight);
    }

    public void RenderBrowserHeader(
        TabStripView tabs,
        BrowserView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme,
        BrowserRenderContext context)
    {
        WriteOperationalHeader(
            tabs,
            keyBindings,
            theme,
            context.Width,
            statusRenderer.BrowserMode(view),
            context.Today,
            view.Projects.FirstOrDefault()?.ActiveCount ?? 0,
            view.Projects.Count(project => project.Error is not null));
    }

    public static void RenderBrowserBody(
        BrowserView view,
        TuiTheme theme,
        BrowserRenderContext context)
    {
        if (context.EditorDialog is not null && context.ContentHeight <= 1)
        {
            return;
        }

        if (context.Width >= 120 && context.Height >= 24)
        {
            WriteWide(view, context.Width, context.ContentHeight, theme, context.Today);
        }
        else if (context.Width >= 80 && context.Height >= 18)
        {
            WriteMedium(view, context.Width, context.ContentHeight, theme, context.Today);
        }
        else
        {
            WriteNarrow(view, context.Width, context.ContentHeight, theme, context.Today);
        }
    }

    public static void RenderBrowserOverlay(
        TuiKeyBindings keyBindings,
        TuiTheme theme,
        BrowserRenderContext context)
    {
        if (context.PomodoroPrompt is { } pomodoroPrompt)
        {
            AnsiConsole.Write(PomodoroPromptRenderer.Render(pomodoroPrompt, theme, context.Width));
        }
        else if (context.SelectList is not null)
        {
            AnsiConsole.Write(SelectList.Default.Render(
                context.SelectList,
                theme,
                new TuiComponentConstraints(context.Width, context.SelectRows)));
        }
        else if (context.TextBox is { } activeTextBox)
        {
            AnsiConsole.Write(MultilineTextBox.Default.Render(
                activeTextBox,
                theme,
                new TuiComponentConstraints(context.Width, context.TextBoxRows),
                TuiKeyBindings.ShortestDisplayName(keyBindings.SaveForm)));
        }
    }

    public static TodoTaskEditorDialogView? CreateBrowserEditorDialog(
        BrowserView view,
        TuiKeyBindings keyBindings,
        int width,
        int height)
    {
        if (view.State.BulkEditor?.FieldTextBox is { } bulkTextBox)
        {
            return new TodoTaskEditorDialogView(
                [new("Enter accepts this field; Esc returns to the bulk form.", TodoTaskEditorDialogRole.Hint)],
                [bulkTextBox],
                Math.Max(3, width - 4));
        }

        return view.State.Editor is { } editor
            ? TodoTaskEditorDialog.Create(editor, keyBindings, width, height)
            : null;
    }

    public static int BrowserContentHeight(
        int terminalHeight,
        int statusHeight,
        int pickerHeight) =>
        Math.Max(1, TerminalLayout.AvailableContentHeight(terminalHeight, statusHeight) - pickerHeight);

    public static MultilineTextBoxState? BrowserTextBox(BrowserView view) =>
        view.State.Editor is null ? null : TodoEditorTextBox(view.State.Editor);

    public static MultilineTextBoxState? TodoEditorTextBox(TodoTaskEditorState editor) => editor.ContentTextBox;

    public static SelectListView? BrowserSelectList(BrowserView view, TuiKeyBindings bindings)
    {
        if (view.CommandPalette is not null)
        {
            return CommandPaletteSelectList(view.CommandPalette, bindings);
        }

        if (view.State.BulkEditor is { FieldTextBox: null } bulkEditor)
        {
            return BulkEditorSelectList(bulkEditor, bindings);
        }

        return view.State.Editor is null
            ? null
            : TodoEditorSelectList(
                view.State.Editor,
                view.Projects
                    .Where(project => project.Project is not null)
                    .Select(project => new TodoEditorProjectOption(project.Title, project.Project!.Path))
                    .ToArray(),
                bindings);
    }

    public static SelectListView CommandPaletteSelectList(CommandPaletteView palette, TuiKeyBindings bindings) =>
        new(
            "Command palette",
            palette.Items.Select(item => new SelectOption(
                $"{item.Group}: {item.Label}",
                $"[{item.Binding}]" + (item.IsEnabled ? string.Empty : $" — {item.DisabledReason}"),
                item.IsEnabled)).ToArray(),
            palette.SelectedIndex,
            palette.State.IsSearching ? palette.State.Query : null,
            "No matching actions",
            CommandPaletteFooter(bindings),
            palette.State.Error);

    public static SelectListView BulkEditorSelectList(
        TodoBulkEditorState editor,
        TuiKeyBindings bindings) => new(
        $"Bulk update {editor.SelectedCount} task(s)",
        [
            new SelectOption("Scheduled date", BulkValue(editor.ScheduledDate, "-", "CLEAR")),
            new SelectOption("Tags", editor.Tags.Length == 0 ? "UNCHANGED" : editor.Tags),
            new SelectOption("Priority", BulkValue(editor.Priority, "-", "CLEAR")),
            new SelectOption("Completion", editor.Complete ? "COMPLETE" : "UNCHANGED")
        ],
        editor.SelectedIndex,
        null,
        "No bulk fields",
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
        $"{Shortest(bindings.Open)} EDIT  {Shortest(bindings.SaveForm)} APPLY  " +
        $"{Shortest(bindings.Back)} CANCEL",
        editor.Error);

    private static string BulkValue(string value, string clearValue, string clearLabel) =>
        value.Length == 0 ? "UNCHANGED" : value == clearValue ? clearLabel : value;

    public static SelectListView? TodoEditorSelectList(
        TodoTaskEditorState editor,
        IReadOnlyList<TodoEditorProjectOption> projects,
        TuiKeyBindings bindings)
    {
        if (editor.IsEditingContent)
        {
            return null;
        }

        if (editor.IsChoosingProject)
        {
            return new SelectListView(
                "Choose project",
                projects.Select(project => new SelectOption(project.Title)).ToArray(),
                editor.ProjectPickerIndex,
                null,
                "No valid projects",
                $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
                $"{Shortest(bindings.Open)} SELECT  {Shortest(bindings.Back)} CANCEL",
                editor.Error);
        }

        return null;
    }

    public static string CommandPaletteFooter(TuiKeyBindings bindings) =>
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
        $"{Shortest(bindings.FilterMode)} SEARCH  {Shortest(bindings.Open)} RUN  " +
        $"{Shortest(bindings.Back)} CLOSE";

    public static void WriteWide(
        BrowserView view,
        int terminalWidth,
        int contentHeight,
        TuiTheme theme,
        DateOnly today)
    {
        const int projectWidth = 22;
        if (!view.State.ShowDetails)
        {
            const int twoPaneFrameAndPaddingWidth = 7;
            var expandedTodoWidth = terminalWidth - projectWidth - twoPaneFrameAndPaddingWidth;
            var hiddenDetailProjectLines = FitLines(
                ProjectLines(view, theme), contentHeight, SelectedProjectIndex(view));
            var expandedTodoLines = FitTodoLines(view, expandedTodoWidth - 2, contentHeight, theme, today);
            var twoPaneTable = CreatePaneTable(theme,
                ("Projects", projectWidth, view.State.Focus == BrowserFocus.Projects, true),
                ($"Todos: {view.SelectedProjectTitle}", expandedTodoWidth,
                    view.State.Focus == BrowserFocus.Todos, true));
            twoPaneTable.AddRow(CreateContent(hiddenDetailProjectLines), CreateContent(expandedTodoLines));
            PadToContentHeight(
                twoPaneTable, contentHeight, hiddenDetailProjectLines.Count, expandedTodoLines.Count);
            WriteSurface(twoPaneTable, theme.Surface, true);
            return;
        }

        const int frameAndPaddingWidth = 10;
        var remainingWidth = terminalWidth - projectWidth - frameAndPaddingWidth;
        var todoWidth = remainingWidth / 2;
        var detailWidth = remainingWidth - todoWidth;
        var projectLines = FitLines(ProjectLines(view, theme), contentHeight, SelectedProjectIndex(view));
        var todoLines = FitTodoLines(view, todoWidth - 2, contentHeight, theme, today);
        var detailLines = FitLines(DetailLines(view, theme), contentHeight, 0);
        var table = CreatePaneTable(theme,
            ("Projects", projectWidth, view.State.Focus == BrowserFocus.Projects, true),
            ($"Todos: {view.SelectedProjectTitle}", todoWidth, view.State.Focus == BrowserFocus.Todos, true),
            ("Details", detailWidth, view.State.Focus == BrowserFocus.Details, false));
        table.AddRow(
            CreateContent(projectLines),
            CreateContent(todoLines),
            OnSurface(CreateContent(detailLines), theme.Surface2, true));
        PadToContentHeight(table, contentHeight, projectLines.Count, todoLines.Count, detailLines.Count);
        WriteSurface(table, theme.Surface, true);
    }

    public static void WriteMedium(
        BrowserView view,
        int terminalWidth,
        int contentHeight,
        TuiTheme theme,
        DateOnly today)
    {
        if (view.State.Focus == BrowserFocus.Projects)
        {
            var projectLines = FitLines(
                ProjectLines(view, theme),
                contentHeight,
                SelectedProjectIndex(view));
            var navigation = CreatePaneTable(theme, ("Navigation", null, true, true));
            navigation.AddRow(CreateContent(projectLines));
            PadToContentHeight(navigation, contentHeight, projectLines.Count);
            WriteSurface(navigation, theme.Surface, true);
            return;
        }

        if (!view.State.ShowDetails)
        {
            var taskLines = FitTodoLines(view, terminalWidth - 4, contentHeight, theme, today);
            var tasks = CreatePaneTable(
                theme,
                ($"Tasks // {view.SelectedProjectTitle}", null, true, true));
            tasks.AddRow(CreateContent(taskLines));
            PadToContentHeight(tasks, contentHeight, taskLines.Count);
            WriteSurface(tasks, theme.Surface, true);
            return;
        }

        const int frameAndPaddingWidth = 7;
        var remainingWidth = terminalWidth - frameAndPaddingWidth;
        var detailWidth = Math.Max(28, remainingWidth * 2 / 5);
        var taskWidth = remainingWidth - detailWidth;
        var todos = FitTodoLines(view, taskWidth - 2, contentHeight, theme, today);
        var details = FitLines(DetailLines(view, theme), contentHeight, 0);
        var table = CreatePaneTable(
            theme,
            ($"Tasks // {view.SelectedProjectTitle}", taskWidth, view.State.Focus == BrowserFocus.Todos, true),
            ("Inspector", detailWidth, view.State.Focus == BrowserFocus.Details, false));
        table.AddRow(CreateContent(todos), OnSurface(CreateContent(details), theme.Surface2, true));
        PadToContentHeight(table, contentHeight, todos.Count, details.Count);
        WriteSurface(table, theme.Surface, true);
    }

    public static void WriteNarrow(
        BrowserView view,
        int terminalWidth,
        int contentHeight,
        TuiTheme theme,
        DateOnly today)
    {
        const int frameAndPaddingWidth = 4;
        var contentWidth = terminalWidth - frameAndPaddingWidth;
        var focus = !view.State.ShowDetails && view.State.Focus == BrowserFocus.Details
            ? BrowserFocus.Todos
            : view.State.Focus;
        var title = focus switch
        {
            BrowserFocus.Projects => "Projects",
            BrowserFocus.Todos => $"Todos: {view.SelectedProjectTitle}",
            _ => "Details"
        };
        var lines = focus switch
        {
            BrowserFocus.Projects => FitLines(ProjectLines(view, theme), contentHeight, SelectedProjectIndex(view)),
            BrowserFocus.Todos => FitTodoLines(view, contentWidth, contentHeight, theme, today),
            _ => FitLines(DetailLines(view, theme), contentHeight, 0)
        };
        var table = CreatePaneTable(theme, (title, null, true, focus != BrowserFocus.Details));
        table.AddRow(focus == BrowserFocus.Details
            ? OnSurface(CreateContent(lines), theme.Surface2, true)
            : CreateContent(lines));
        PadToContentHeight(table, contentHeight, lines.Count);

        WriteSurface(table, theme.Surface, true);
    }

    public static Table CreatePaneTable(
        TuiTheme theme,
        params (string Title, int? Width, bool Focused, bool NoWrap)[] panes)
    {
        var table = new Table().SquareBorder().Expand();
        table.BorderStyle = ThemeStyle(
            panes.Any(pane => pane.Focused) ? theme.BorderActive : theme.Border);

        foreach (var pane in panes)
        {
            var header = new Text(
                pane.Title.ToUpperInvariant(),
                ThemeStyle(pane.Focused ? theme.Accent : theme.Heading, Decoration.Bold));
            table.AddColumn(new TableColumn(header)
            {
                Width = pane.Width,
                NoWrap = pane.NoWrap,
                Padding = new Padding(1, 0)
            });
        }

        return table;
    }

    public static void WriteOperationalHeader(
        TabStripView view,
        TuiKeyBindings bindings,
        TuiTheme theme,
        int terminalWidth,
        string mode,
        DateOnly date,
        int openCount,
        int errorCount)
    {
        var segments = new List<(string Text, Color Color, Decoration Decoration)>();
        if (terminalWidth >= 60)
        {
            segments.Add(("WOLF TODO // ", theme.Heading, Decoration.Bold));
            for (var index = 0; index < view.Tabs.Length; index++)
            {
                if (index > 0)
                {
                    segments.Add(("  ", theme.Text, Decoration.None));
                }

                var tab = view.Tabs[index];
                var title = tab.IsSelected
                    ? $"[{tab.Title.ToUpperInvariant()}]"
                    : tab.Title.ToUpperInvariant();
                var color = tab.IsSelected ? theme.Accent : theme.Muted;
                var decoration = tab.IsSelected ? Decoration.Bold : Decoration.Dim;
                segments.Add((title, color, decoration));
            }
        }
        else
        {
            var active = view.Tabs.First(tab => tab.IsSelected);
            segments.Add(($"[{active.Title.ToUpperInvariant()}]", theme.Accent, Decoration.Bold));
        }

        segments.Add(($"  MODE:{mode}", theme.SecondaryText, Decoration.None));
        if (terminalWidth >= 80)
        {
            segments.Add(($"  {date.ToString("ddd dd MMM").ToUpperInvariant()}", theme.Date, Decoration.None));
        }

        if (terminalWidth >= 100)
        {
            segments.Add(($"  OPEN:{openCount}", theme.Text, Decoration.None));
            segments.Add((
                errorCount == 0 ? "  FILES:CLEAN" : $"  FILES:{errorCount} ERRORS",
                errorCount == 0 ? theme.Muted : theme.Error,
                errorCount == 0 ? Decoration.Dim : Decoration.Bold));
        }

        if (terminalWidth >= 120 && view.Tabs.Length > 1)
        {
            var hint = $"  {TuiKeyBindings.ShortestDisplayName(bindings.TabPrevious)}/" +
                       $"{TuiKeyBindings.ShortestDisplayName(bindings.TabNext)} TABS";
            segments.Add((hint, theme.Muted, Decoration.Dim));
        }

        var totalLength = segments.Sum(segment => segment.Text.Length);
        var width = Math.Max(1, terminalWidth);
        var remaining = totalLength > width ? width - 1 : width;
        var output = new System.Text.StringBuilder();

        foreach (var segment in segments)
        {
            var length = Math.Min(segment.Text.Length, remaining);
            if (length == 0)
            {
                break;
            }

            AppendStyled(output, segment.Text[..length], segment.Color, segment.Decoration);
            remaining -= length;
        }

        if (totalLength > width)
        {
            AppendStyled(output, "…", theme.Muted);
        }

        WriteSurface(new Markup(output.ToString()), theme.Background, true);
        AnsiConsole.WriteLine();
    }

    public static string BrowserMode(BrowserView view) => view switch
    {
        { CommandPalette: not null } => "HELP",
        { GlobalCommand: not null } => "COMMAND",
        { GlobalError: not null } => "ERROR",
        { State.Editor.IsCreate: true } => "CREATE",
        { State.Editor: not null } => "EDIT",
        { State.IsFilterMode: true } => "FILTER",
        { State.IsSortMode: true } => "SORT",
        { State.Error: not null } => "ERROR",
        _ => "BROWSE"
    };

    public static IReadOnlyList<IRenderable> ProjectLines(BrowserView view, TuiTheme theme)
    {
        return view.Projects.Select(row =>
        {
            var line = new System.Text.StringBuilder();
            var rowColor = row.IsSelected
                ? theme.AccentBright
                : row.Kind is ProjectRowKind.Today or ProjectRowKind.SavedQuery ? theme.Date : theme.Text;
            AppendStyled(
                line,
                row.IsSelected ? ">" : " ",
                rowColor,
                row.IsSelected ? Decoration.Bold : Decoration.None);
            AppendStyled(
                line,
                row.Error is null ? " " : "!",
                row.Error is null ? rowColor : theme.Error,
                row.Error is null ? Decoration.None : Decoration.Bold);
            AppendStyled(line, $" {row.Title}", row.Error is null ? rowColor : theme.Error);
            if (row.Error is null)
            {
                AppendStyled(line, $" {row.ActiveCount}", theme.SecondaryText, Decoration.Dim);
            }

            var content = (IRenderable)new Markup(line.ToString()).Ellipsis();
            return row.IsSelected
                ? OnSurface(content, theme.Surface2, true)
                : content;
        }).ToArray();
    }

    public static IReadOnlyList<TodoLineGroup> TodoLineGroups(
        BrowserView view,
        int contentWidth,
        TuiTheme theme,
        DateOnly today)
    {
        if (view.Diagnostic is not null)
        {
            return
            [
                new TodoLineGroup(
                    [new Text("Select the error entry for details.", ThemeStyle(theme.Error))],
                    true)
            ];
        }

        if (view.Todos.Length == 0)
        {
            return [new TodoLineGroup([new Text(view.EmptyMessage, ThemeStyle(theme.Muted))], true)];
        }

        var layout = TodoRowRenderer.Default.Columns(contentWidth, view.SelectedProjectPath is null);
        var groups = new List<TodoLineGroup>
        {
            new([TodoRowRenderer.Default.ColumnHeader(layout, theme)], false)
        };
        groups.AddRange(view.Todos.Select(row =>
        {
            if (row.Heading is not null)
            {
                return new TodoLineGroup(
                    [new Text(row.Heading, ThemeStyle(theme.Heading, Decoration.Bold)).Ellipsis()],
                    false);
            }

            var item = new TodoListItemState(row, view.SelectedProjectPath is null, contentWidth);
            return new TodoLineGroup(
                TodoListItem.Default.RenderLines(item, theme),
                row.IsSelected);
        }));
        return groups;
    }

    public static IReadOnlyList<IRenderable> DetailLines(BrowserView view, TuiTheme theme)
    {
        var lines = new List<IRenderable>();

        if (view.Diagnostic is not null)
        {
            lines.Add(new Text("PROJECT ERROR", ThemeStyle(theme.Error, Decoration.Bold)));
            lines.Add(new Text(view.SelectedProjectPath ?? string.Empty, ThemeStyle(theme.Muted)));
            lines.Add(new Text(string.Empty));
            lines.Add(new Text(view.Diagnostic, ThemeStyle(theme.Error)));
        }
        else if (view.SelectedTodo is null)
        {
            lines.Add(new Text(view.EmptyMessage, ThemeStyle(theme.Muted)));
        }
        else
        {
            var todo = view.SelectedTodo;
            lines.Add(new Text(todo.Title, ThemeStyle(theme.Heading, Decoration.Bold)));
            AddField(lines, "Project", view.SelectedProjectTitle, theme, theme.Text);

            if (!string.IsNullOrEmpty(todo.SectionPath))
            {
                AddField(lines, "Section", todo.SectionPath, theme, theme.Text);
            }

            AddField(lines, "Reference", todo.ExternalReference, theme, theme.Info);
            AddField(
                lines,
                "Priority",
                todo.Priority?.ToString(),
                theme,
                PriorityColor(todo.Priority, theme));
            AddField(
                lines,
                "Tags",
                todo.Tags.Length == 0 ? null : string.Join(", ", todo.Tags.Select(tag => $"#{tag}")),
                theme,
                theme.Tag);
            AddField(
                lines,
                "Scheduled",
                todo.Schedule is null
                    ? null
                    : FormatSchedule(todo.Schedule),
                theme,
                theme.Date);
            AddField(lines, "Duration", FormatDuration(todo.Duration), theme, theme.Info);

            if (todo.Notes.Length == 0 && todo.Subtasks.Length == 0)
            {
                lines.Add(new Text(string.Empty));
                lines.Add(new Text("NO ADDITIONAL DETAILS", ThemeStyle(theme.Muted)));
            }
            else
            {
                if (todo.Notes.Length > 0)
                {
                    lines.Add(new Text(string.Empty));
                    lines.Add(new Text("NOTES", ThemeStyle(theme.Heading, Decoration.Bold)));
                    lines.AddRange(todo.Notes.Select(note => new Text($"• {note.Text}", ThemeStyle(theme.Text))));
                }

                if (todo.Subtasks.Length > 0)
                {
                    lines.Add(new Text(string.Empty));
                    lines.Add(new Text("SUBTASKS", ThemeStyle(theme.Heading, Decoration.Bold)));
                    lines.AddRange(FlattenSubtasks(todo.Subtasks)
                        .Select(item => DetailedTodoLine(item.Todo, item.TreePath, false, theme)));
                }
            }
        }

        return lines;
    }

    public static IRenderable CreateContent(IReadOnlyList<IRenderable> lines)
    {
        return lines.Count == 0 ? new Text(string.Empty) : new Rows(lines);
    }

    public static int AvailableContentHeight(int terminalHeight, int statusLineCount) =>
        TerminalLayout.AvailableContentHeight(terminalHeight, statusLineCount);

    public static int? DialogContentHeight(TodoTaskEditorDialogView? dialog) =>
        TerminalLayout.DialogContentHeight(dialog);

    public static IReadOnlyList<IRenderable> FitLines(
        IReadOnlyList<IRenderable> lines,
        int contentHeight,
        int selectedIndex)
    {
        if (lines.Count <= contentHeight)
        {
            return lines;
        }

        var start = Math.Clamp(selectedIndex - contentHeight + 1, 0, lines.Count - contentHeight);
        return lines.Skip(start).Take(contentHeight).ToArray();
    }

    public static IReadOnlyList<IRenderable> FitTodoLines(
        BrowserView view,
        int contentWidth,
        int contentHeight,
        TuiTheme theme,
        DateOnly today)
    {
        var allGroups = TodoLineGroups(view, contentWidth, theme, today);
        var hasColumnHeader = view.Diagnostic is null && view.Todos.Length > 0;
        var header = hasColumnHeader ? allGroups[0].Lines : [];
        var groups = hasColumnHeader ? allGroups.Skip(1).ToArray() : allGroups.ToArray();
        var availableHeight = Math.Max(0, contentHeight - header.Count);
        if (availableHeight == 0)
        {
            return header;
        }

        if (groups.Sum(group => group.Lines.Count) <= availableHeight)
        {
            return header.Concat(groups.SelectMany(group => group.Lines)).ToArray();
        }

        var selectedIndex = 0;
        for (var index = 0; index < groups.Length; index++)
        {
            if (groups[index].IsSelected)
            {
                selectedIndex = index;
                break;
            }
        }

        var selected = groups[selectedIndex];
        if (selected.Lines.Count > availableHeight)
        {
            return header.Concat(selected.Lines.Take(availableHeight)).ToArray();
        }

        var start = selectedIndex;
        var end = selectedIndex;
        var usedHeight = selected.Lines.Count;

        var rowsBelowSelection = selected.Lines.Count - 1;
        while (end + 1 < groups.Length &&
               rowsBelowSelection < TodoSelectionLookAheadRows &&
               usedHeight + groups[end + 1].Lines.Count <= availableHeight)
        {
            end++;
            usedHeight += groups[end].Lines.Count;
            rowsBelowSelection += groups[end].Lines.Count;
        }

        while (start > 0 && usedHeight + groups[start - 1].Lines.Count <= availableHeight)
        {
            start--;
            usedHeight += groups[start].Lines.Count;
        }

        while (end + 1 < groups.Length && usedHeight + groups[end + 1].Lines.Count <= availableHeight)
        {
            end++;
            usedHeight += groups[end].Lines.Count;
        }

        return header.Concat(groups.Skip(start).Take(end - start + 1).SelectMany(group => group.Lines)).ToArray();
    }

    public static int SelectedProjectIndex(BrowserView view)
    {
        for (var index = 0; index < view.Projects.Length; index++)
        {
            if (view.Projects[index].IsSelected)
            {
                return index;
            }
        }

        return 0;
    }

    public static void PadToContentHeight(Table table, int contentHeight, params int[] paneLineCounts)
    {
        var renderedContentHeight = Math.Max(1, paneLineCounts.Max());

        for (var row = renderedContentHeight; row < contentHeight; row++)
        {
            table.AddEmptyRow();
        }
    }

    public static string FitColumn(string value, int width)
    {
        var result = Truncate(value, width);
        return result + new string(' ', Math.Max(0, width - DisplayWidth(result)));
    }

    public static string FormatSchedule(TodoSchedule schedule) =>
        schedule.Time is null
            ? schedule.Date.ToString("yyyy-MM-dd")
            : $"{schedule.Date:yyyy-MM-dd} {schedule.Time:HH:mm}";

    public static string? FormatDuration(TimeSpan? duration) => duration is null
        ? null
        : $"{(int)duration.Value.TotalMinutes}m";

    public static string PriorityCode(TodoPriority? priority) => priority switch
    {
        TodoPriority.Highest => "!",
        TodoPriority.High => "H",
        TodoPriority.Medium => "M",
        TodoPriority.Low => "L",
        TodoPriority.Lowest => ".",
        _ => "-"
    };

    public static string TodoStatusGlyph(bool isCompleted) =>
        isCompleted ? CompletedTodoGlyph : OpenTodoGlyph;

    public static string Truncate(string value, int width)
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

    public static int DisplayWidth(string value) => value.GetCellWidth();

    public static IEnumerable<(TodoItem Todo, ImmutableArray<TodoTreeSegment> TreePath)> FlattenSubtasks(
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

    public static IRenderable DetailedTodoLine(
        TodoItem todo,
        ImmutableArray<TodoTreeSegment> treePath,
        bool selected,
        TuiTheme theme)
    {
        var cursor = selected ? ">" : " ";
        var treePrefix = TodoTreeFormatter.Format(treePath);
        var status = TodoStatusGlyph(todo.IsCompleted);
        var reference = todo.ExternalReference is null ? string.Empty : $"{todo.ExternalReference} - ";
        var priority = PriorityCode(todo.Priority);
        var tags = todo.Tags.Length == 0 ? string.Empty : $" {string.Join(' ', todo.Tags.Select(tag => $"#{tag}"))}";
        var schedule = todo.Schedule is null
            ? string.Empty
            : $" ⏳ {FormatSchedule(todo.Schedule)}";

        var line = new System.Text.StringBuilder();
        AppendStyled(
            line,
            cursor,
            selected ? theme.Accent : todo.IsCompleted ? theme.Muted : theme.Text,
            selected ? Decoration.Bold : todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(
            line,
            $" {treePrefix}",
            selected ? theme.Accent : theme.Muted,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(
            line,
            status,
            todo.IsCompleted ? theme.Muted : theme.Accent,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(line, $" {priority}", todo.IsCompleted ? theme.Muted : PriorityColor(todo.Priority, theme));
        AppendStyled(
            line,
            $" {reference}{todo.Title}",
            todo.IsCompleted ? theme.Muted : theme.Text,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(line, tags, todo.IsCompleted ? theme.Muted : theme.Tag,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(line, schedule, todo.IsCompleted ? theme.Muted : theme.Date,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        return new Markup(line.ToString());
    }

    public static Color PriorityColor(TodoPriority? priority, TuiTheme theme) => priority switch
    {
        TodoPriority.Highest => theme.Error,
        TodoPriority.High => theme.Warning,
        TodoPriority.Medium => theme.Accent,
        TodoPriority.Low => theme.Muted,
        TodoPriority.Lowest => theme.Muted,
        _ => theme.Text
    };

    public static void AddField(
        List<IRenderable> lines,
        string name,
        string? value,
        TuiTheme theme,
        Color valueColor)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var line = new System.Text.StringBuilder();
            AppendStyled(line, $"{name.ToUpperInvariant()}: ", theme.Heading, Decoration.Bold);
            AppendStyled(line, value, valueColor);
            lines.Add(new Markup(line.ToString()));
        }
    }

    public static string Shortest(System.Collections.Immutable.ImmutableArray<KeyGesture> gestures) =>
        TuiKeyBindings.ShortestDisplayName(gestures);

    public static Style ThemeStyle(
        Color color,
        Decoration decoration = Decoration.None,
        Color? background = null) =>
        new(color, background ?? Color.Default, decoration);

    public static IRenderable OnSurface(IRenderable content, Color background, bool expand = false) =>
        new SurfaceThemeRenderer().OnSurface(content, background, expand);

    public static void WriteSurface(IRenderable content, Color background, bool expand = false) =>
        new SurfaceThemeRenderer().WriteSurface(content, background, expand);

    public static void AppendStyled(
        System.Text.StringBuilder output,
        string value,
        Color color,
        Decoration decoration = Decoration.None,
        Color? background = null)
    {
        if (value.Length == 0)
        {
            return;
        }

        var styles = new List<string>();
        if (color != Color.Default)
        {
            styles.Add(color.ToMarkup());
        }

        if (background is not null && background != Color.Default)
        {
            styles.Add($"on {background.Value.ToMarkup()}");
        }

        if ((decoration & Decoration.Bold) != 0)
        {
            styles.Add("bold");
        }

        if ((decoration & Decoration.Dim) != 0)
        {
            styles.Add("dim");
        }

        var content = Markup.Escape(value);
        if (styles.Count == 0)
        {
            output.Append(content);
            return;
        }

        output.Append('[');
        output.AppendJoin(' ', styles);
        output.Append(']');
        output.Append(content);
        output.Append("[/]");
    }

    public static int SafeWindowWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch (IOException)
        {
            return 80;
        }
    }

    public static int SafeWindowHeight()
    {
        try
        {
            return Console.WindowHeight;
        }
        catch (IOException)
        {
            return 24;
        }
    }
}
