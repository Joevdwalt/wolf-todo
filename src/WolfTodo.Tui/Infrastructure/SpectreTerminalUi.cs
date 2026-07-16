using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Infrastructure;

public sealed class SpectreTerminalUi : ITerminalUi
{
    private readonly Func<int> widthProvider;
    private readonly Func<int> heightProvider;
    private bool browserRendered;

    public SpectreTerminalUi() : this(SafeWindowWidth, SafeWindowHeight)
    {
    }

    public SpectreTerminalUi(Func<int> widthProvider, Func<int> heightProvider)
    {
        this.widthProvider = widthProvider;
        this.heightProvider = heightProvider;
    }

    public void ShowSplash(string logo) => ShowSplash(logo, TuiThemes.Wolf);

    public void ShowSplash(string logo, TuiTheme theme)
    {
        browserRendered = false;
        AnsiConsole.Clear();

        var content = new Rows(
            new Text(logo, ThemeStyle(theme.Accent)),
            new Text(string.Empty),
            new Text("Wolf Todo", ThemeStyle(theme.Heading, Decoration.Bold)),
            new Text("Press any key to continue", ThemeStyle(theme.Muted, Decoration.Dim)));

        if (widthProvider() < LongestLine(logo) || heightProvider() < 5)
        {
            AnsiConsole.Write(new Text("Wolf Todo\n", ThemeStyle(theme.Heading, Decoration.Bold)));
            AnsiConsole.Write(new Text("Press any key to continue\n", ThemeStyle(theme.Muted, Decoration.Dim)));
            return;
        }

        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle));
    }

    public void ShowBrowser(TabStripView tabs, BrowserView view, TuiKeyBindings keyBindings) =>
        ShowBrowser(tabs, view, keyBindings, TuiThemes.Wolf);

    public void ShowBrowser(
        TabStripView tabs,
        BrowserView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme)
    {
        var useSynchronizedUpdate = browserRendered && AnsiConsole.Profile.Out.IsTerminal;

        if (browserRendered)
        {
            BeginUpdate(useSynchronizedUpdate);
        }
        else
        {
            AnsiConsole.Clear();
            browserRendered = true;
        }

        var width = widthProvider();
        var height = heightProvider();
        var compact = width < 80 || height < 18;
        var statusLines = CreateStatusLines(view, keyBindings, compact, width, height);
        var contentHeight = AvailableContentHeight(height, statusLines.Count);
        WriteTabStrip(tabs, keyBindings, theme, width);

        if (width >= 120 && height >= 24)
        {
            WriteWide(view, width, contentHeight, theme);
        }
        else if (width >= 80 && height >= 18)
        {
            WriteMedium(view, width, contentHeight, theme);
        }
        else
        {
            WriteNarrow(view, width, contentHeight, theme);
        }

        WriteStatus(statusLines, view, theme);
        EndUpdate(useSynchronizedUpdate);
    }

    public void ShowPlanner(
        TabStripView tabs,
        PlannerView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme)
    {
        var useSynchronizedUpdate = browserRendered && AnsiConsole.Profile.Out.IsTerminal;
        if (browserRendered)
        {
            BeginUpdate(useSynchronizedUpdate);
        }
        else
        {
            AnsiConsole.Clear();
            browserRendered = true;
        }

        var width = widthProvider();
        var height = heightProvider();
        WriteTabStrip(tabs, keyBindings, theme, width);
        var status = PlannerStatus(view, keyBindings, width, height);
        var pickerVisible = view.State.Mode is PlannerMode.ChooseTodo or PlannerMode.EditFilter;
        const int tabTableStatusBorderAndCursorHeight = 10;
        const int pickerHeight = 3;
        var reservedHeight = tabTableStatusBorderAndCursorHeight + (pickerVisible ? pickerHeight : 0);
        var availableRows = Math.Max(1, height - status.Count - reservedHeight);
        var visibleSlots = WindowPlannerSlots(view.Slots, view.State.SlotIndex, availableRows);
        var table = new Table().RoundedBorder().Expand();
        table.BorderStyle = ThemeStyle(theme.Border);
        table.AddColumn(new TableColumn(new Text(
            view.State.SelectedDate.ToString("ddd yyyy-MM-dd"),
            ThemeStyle(theme.Heading, Decoration.Bold)))
        {
            Width = 8,
            NoWrap = true
        });
        table.AddColumn(new TableColumn(new Text("Plan", ThemeStyle(theme.Accent, Decoration.Bold))));
        foreach (var slot in visibleSlots)
        {
            var selectedColor = slot.IsSelected ? theme.Accent : theme.Date;
            var time = new Text(
                slot.Time.ToString("HH:mm"),
                ThemeStyle(selectedColor, slot.IsSelected ? Decoration.Bold : Decoration.None));
            IRenderable content;
            if (slot.Assignments.Length > 1)
            {
                content = new Text(
                    $"! {slot.Assignments.Length} conflicting assignments",
                    ThemeStyle(theme.Error, Decoration.Bold));
            }
            else if (slot.Assignments.Length == 1)
            {
                var assignment = slot.Assignments[0];
                var prefix = slot.IsSelected ? "> " : "  ";
                content = new Text(
                    $"{prefix}{assignment.Todo.Title}  [{assignment.ProjectTitle}]",
                    ThemeStyle(
                        assignment.Todo.IsCompleted ? theme.Success : slot.IsSelected ? theme.Accent : theme.Text,
                        assignment.Todo.IsCompleted ? Decoration.Dim : slot.IsSelected ? Decoration.Bold : Decoration.None))
                    .Ellipsis();
            }
            else
            {
                content = new Text(slot.IsSelected ? "> —" : "  —", ThemeStyle(theme.Muted, Decoration.Dim));
            }

            table.AddRow(time, content);
        }

        for (var index = visibleSlots.Count; index < availableRows; index++)
        {
            table.AddEmptyRow();
        }

        AnsiConsole.Write(table);
        if (view.State.Mode is PlannerMode.ChooseTodo or PlannerMode.EditFilter)
        {
            WritePlannerPicker(view, theme, width);
        }

        WritePlannerStatus(status, view, theme);
        EndUpdate(useSynchronizedUpdate);
    }

    private static IReadOnlyList<PlannerSlotView> WindowPlannerSlots(
        IReadOnlyList<PlannerSlotView> slots,
        int selectedIndex,
        int availableRows)
    {
        if (slots.Count <= availableRows)
        {
            return slots;
        }

        var start = Math.Clamp(selectedIndex - availableRows + 1, 0, slots.Count - availableRows);
        return slots.Skip(start).Take(availableRows).ToArray();
    }

    private static IReadOnlyList<string> PlannerStatus(
        PlannerView view,
        TuiKeyBindings bindings,
        int terminalWidth,
        int terminalHeight)
    {
        IReadOnlyList<string> status;
        if (view.CommandPalette is not null)
        {
            return CommandPaletteStatus(view.CommandPalette, bindings, terminalWidth, terminalHeight);
        }

        if (view.GlobalCommand is not null)
        {
            status = [view.GlobalCommand];
        }
        else if (view.GlobalError is not null)
        {
            status = [view.GlobalError];
        }
        else if (view.State.Error is not null)
        {
            status = [view.State.Error];
        }
        else
        {
            status = view.State.Mode switch
            {
                PlannerMode.EditFilter => [$"/{view.State.FilterDraft}"],
                PlannerMode.ChooseTodo =>
                [
                    $"Choose todo  {Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} move  " +
                    $"{Shortest(bindings.Open)} assign  {Shortest(bindings.FilterMode)} filter  " +
                    $"{Shortest(bindings.Back)} cancel"
                ],
                PlannerMode.MoveTodo =>
                [
                    $"Move todo  {Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} slot  " +
                    $"{Shortest(bindings.PlannerPreviousDay)}/{Shortest(bindings.PlannerNextDay)} day  " +
                    $"{Shortest(bindings.Open)} place  {Shortest(bindings.Back)} cancel"
                ],
                PlannerMode.ChooseCreateProject =>
                [
                    view.Projects.Length == 0
                        ? "No valid projects"
                        : $"Create in: {view.Projects[view.State.CreateProjectIndex].Title}  " +
                          $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} move  " +
                          $"{Shortest(bindings.Open)} select  {Shortest(bindings.Back)} cancel"
                ],
                PlannerMode.EnterCreateTitle =>
                [
                    $"New todo title: {view.State.CreateTitleDraft}_  Enter save  Esc cancel"
                ],
                _ =>
                [
                    $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} slot  " +
                    $"{Shortest(bindings.PlannerPreviousDay)}/{Shortest(bindings.PlannerNextDay)} day  " +
                    $"{Shortest(bindings.PlannerToday)} today  {Shortest(bindings.Open)} assign/move  " +
                    $"{Shortest(bindings.PlannerUnschedule)} unschedule  " +
                    $"{Shortest(bindings.CreateTodo)} create"
                ]
            };
        }

        var statusWidth = Math.Max(1, terminalWidth - 4);
        return status.SelectMany(line => WrapStatus(line, statusWidth)).ToArray();
    }

    private static void WritePlannerPicker(PlannerView view, TuiTheme theme, int width)
    {
        var text = view.SelectedPickerTodo is null
            ? "No open unscheduled todos"
            : $"> {view.SelectedPickerTodo.Todo.Title}  [{view.SelectedPickerTodo.ProjectTitle}]";
        if (text.GetCellWidth() > Math.Max(1, width - 4))
        {
            text = Truncate(text, Math.Max(1, width - 4));
        }

        AnsiConsole.Write(new Panel(new Text(
            text,
            ThemeStyle(view.SelectedPickerTodo is null ? theme.Muted : theme.Accent)))
        {
            Header = new PanelHeader("Unscheduled todos"),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeStyle(theme.Border),
            Expand = true
        });
    }

    private static void WritePlannerStatus(
        IReadOnlyList<string> lines,
        PlannerView view,
        TuiTheme theme)
    {
        var style = view.GlobalError is not null || view.State.Error is not null ||
                    view.CommandPalette?.State.Error is not null
            ? ThemeStyle(theme.Error, Decoration.Bold)
            : view.GlobalCommand is not null || view.CommandPalette is not null
                ? ThemeStyle(theme.Accent)
            : view.State.Mode == PlannerMode.Browse
                ? ThemeStyle(theme.Muted, Decoration.Dim)
                : ThemeStyle(theme.Accent);
        AnsiConsole.Write(new Panel(new Rows(lines.Select(line => new Text(line, style))))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeStyle(theme.Border),
            Expand = true
        });
    }

    public void ShowStartupError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Startup error:[/] {Markup.Escape(message)}");
    }

    public void SetCursorVisible(bool visible)
    {
        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return;
        }

        var writer = AnsiConsole.Profile.Out.Writer;
        writer.Write(visible ? "\u001b[?25h" : "\u001b[?25l");
        writer.Flush();
    }

    public void SuspendForExternalProcess()
    {
        SetCursorVisible(true);
        browserRendered = false;
        AnsiConsole.Clear();
    }

    public void ResumeAfterExternalProcess()
    {
        browserRendered = false;
        AnsiConsole.Clear();
        SetCursorVisible(false);
    }

    public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

    private static void BeginUpdate(bool synchronized)
    {
        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return;
        }

        var writer = AnsiConsole.Profile.Out.Writer;

        if (synchronized)
        {
            writer.Write("\u001b[?2026h");
        }

        writer.Write("\u001b[H");
    }

    private static void EndUpdate(bool synchronized)
    {
        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return;
        }

        var writer = AnsiConsole.Profile.Out.Writer;
        writer.Write("\u001b[J");

        if (synchronized)
        {
            writer.Write("\u001b[?2026l");
        }

        writer.Flush();
    }

    private static void WriteWide(BrowserView view, int terminalWidth, int contentHeight, TuiTheme theme)
    {
        const int projectWidth = 22;
        if (!view.State.ShowDetails)
        {
            const int twoPaneFrameAndPaddingWidth = 7;
            var expandedTodoWidth = terminalWidth - projectWidth - twoPaneFrameAndPaddingWidth;
            var hiddenDetailProjectLines = FitLines(
                ProjectLines(view, theme), contentHeight, SelectedProjectIndex(view));
            var expandedTodoLines = FitTodoLines(view, expandedTodoWidth - 2, contentHeight, theme);
            var twoPaneTable = CreatePaneTable(theme,
                ("Projects", projectWidth, view.State.Focus == BrowserFocus.Projects, true),
                ($"Todos: {view.SelectedProjectTitle}", expandedTodoWidth,
                    view.State.Focus == BrowserFocus.Todos, true));
            twoPaneTable.AddRow(CreateContent(hiddenDetailProjectLines), CreateContent(expandedTodoLines));
            PadToContentHeight(
                twoPaneTable, contentHeight, hiddenDetailProjectLines.Count, expandedTodoLines.Count);
            AnsiConsole.Write(twoPaneTable);
            return;
        }

        const int frameAndPaddingWidth = 10;
        var remainingWidth = terminalWidth - projectWidth - frameAndPaddingWidth;
        var todoWidth = remainingWidth / 2;
        var detailWidth = remainingWidth - todoWidth;
        var projectLines = FitLines(ProjectLines(view, theme), contentHeight, SelectedProjectIndex(view));
        var todoLines = FitTodoLines(view, todoWidth - 2, contentHeight, theme);
        var detailLines = FitLines(DetailLines(view, theme), contentHeight, 0);
        var table = CreatePaneTable(theme,
            ("Projects", projectWidth, view.State.Focus == BrowserFocus.Projects, true),
            ($"Todos: {view.SelectedProjectTitle}", todoWidth, view.State.Focus == BrowserFocus.Todos, true),
            ("Details", detailWidth, view.State.Focus == BrowserFocus.Details, false));
        table.AddRow(
            CreateContent(projectLines),
            CreateContent(todoLines),
            CreateContent(detailLines));
        PadToContentHeight(table, contentHeight, projectLines.Count, todoLines.Count, detailLines.Count);
        AnsiConsole.Write(table);
    }

    private static void WriteMedium(BrowserView view, int terminalWidth, int contentHeight, TuiTheme theme)
    {
        const int projectWidth = 22;
        const int frameAndPaddingWidth = 7;
        var contentWidth = terminalWidth - projectWidth - frameAndPaddingWidth;
        var showDetails = view.State.ShowDetails && view.State.Focus == BrowserFocus.Details;
        var projectLines = FitLines(ProjectLines(view, theme), contentHeight, SelectedProjectIndex(view));
        var contentLines = showDetails
            ? FitLines(DetailLines(view, theme), contentHeight, 0)
            : FitTodoLines(view, contentWidth - 2, contentHeight, theme);
        var table = CreatePaneTable(theme,
            ("Projects", projectWidth, view.State.Focus == BrowserFocus.Projects, true),
            (showDetails ? "Details" : $"Todos: {view.SelectedProjectTitle}", contentWidth, true, !showDetails));
        table.AddRow(
            CreateContent(projectLines),
            CreateContent(contentLines));
        PadToContentHeight(table, contentHeight, projectLines.Count, contentLines.Count);
        AnsiConsole.Write(table);
    }

    private static void WriteNarrow(BrowserView view, int terminalWidth, int contentHeight, TuiTheme theme)
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
            BrowserFocus.Todos => FitTodoLines(view, contentWidth, contentHeight, theme),
            _ => FitLines(DetailLines(view, theme), contentHeight, 0)
        };
        var table = CreatePaneTable(theme, (title, null, true, focus != BrowserFocus.Details));
        table.AddRow(CreateContent(lines));
        PadToContentHeight(table, contentHeight, lines.Count);

        AnsiConsole.Write(table);
    }

    private static Table CreatePaneTable(
        TuiTheme theme,
        params (string Title, int? Width, bool Focused, bool NoWrap)[] panes)
    {
        var table = new Table().RoundedBorder().Expand();
        table.BorderStyle = ThemeStyle(theme.Border);

        foreach (var pane in panes)
        {
            var header = new Text(
                pane.Title,
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

    private static void WriteTabStrip(
        TabStripView view,
        TuiKeyBindings bindings,
        TuiTheme theme,
        int terminalWidth)
    {
        var segments = new List<(string Text, Color Color, Decoration Decoration)>();
        for (var index = 0; index < view.Tabs.Length; index++)
        {
            if (index > 0)
            {
                segments.Add(("  ", theme.Text, Decoration.None));
            }

            var tab = view.Tabs[index];
            var title = tab.IsSelected ? $"[ {tab.Title} ]" : $"  {tab.Title}  ";
            var color = tab.IsSelected ? theme.Accent : theme.Muted;
            var decoration = tab.IsSelected ? Decoration.Bold : Decoration.Dim;
            segments.Add((title, color, decoration));
        }

        if (view.Tabs.Length > 1)
        {
            var hint = $"  {TuiKeyBindings.ShortestDisplayName(bindings.TabNext)} tabs";
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

        AnsiConsole.Write(new Markup(output.ToString()));
        AnsiConsole.WriteLine();
    }

    private static IReadOnlyList<IRenderable> ProjectLines(BrowserView view, TuiTheme theme)
    {
        return view.Projects.Select(row =>
        {
            var line = new System.Text.StringBuilder();
            AppendStyled(
                line,
                row.IsSelected ? ">" : " ",
                row.IsSelected ? theme.Accent : theme.Text,
                row.IsSelected ? Decoration.Bold : Decoration.None);
            AppendStyled(
                line,
                row.Error is null ? " " : "!",
                row.Error is null ? theme.Text : theme.Error,
                row.Error is null ? Decoration.None : Decoration.Bold);
            AppendStyled(line, $" {row.Title}", row.Error is null ? theme.Text : theme.Error);
            if (row.Error is null)
            {
                AppendStyled(line, $" {row.ActiveCount}", theme.Muted, Decoration.Dim);
            }

            return (IRenderable)new Markup(line.ToString()).Ellipsis();
        }).ToArray();
    }

    private static IReadOnlyList<TodoLineGroup> TodoLineGroups(
        BrowserView view,
        int contentWidth,
        TuiTheme theme)
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

        return view.Todos.Select(row =>
        {
            if (row.Heading is not null)
            {
                return new TodoLineGroup(
                    [new Text(row.Heading, ThemeStyle(theme.Heading, Decoration.Bold)).Ellipsis()],
                    false);
            }

            var lines = new List<IRenderable> { TodoListRow(row, contentWidth, theme) };
            if (row.Todo!.Schedule is not null)
            {
                lines.Add(TodoScheduleLine(row, contentWidth, theme));
            }

            return new TodoLineGroup(lines, row.IsSelected);
        }).ToArray();
    }

    private static IReadOnlyList<IRenderable> DetailLines(BrowserView view, TuiTheme theme)
    {
        var lines = new List<IRenderable>();

        if (view.Diagnostic is not null)
        {
            lines.Add(new Text("Project error", ThemeStyle(theme.Error, Decoration.Bold)));
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

            AddField(lines, "Reference", todo.ExternalReference, theme, theme.Text);
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
            AddField(lines, "Start", todo.StartDate?.ToString("yyyy-MM-dd"), theme, theme.Date);
            AddField(lines, "Due", todo.DueDate?.ToString("yyyy-MM-dd"), theme, theme.Date);
            AddField(
                lines,
                "Scheduled",
                todo.Schedule is null
                    ? null
                    : $"{todo.Schedule.Date:yyyy-MM-dd} {todo.Schedule.Time:HH:mm}",
                theme,
                theme.Date);

            if (todo.Notes.Length == 0 && todo.Subtasks.Length == 0)
            {
                lines.Add(new Text(string.Empty));
                lines.Add(new Text("No additional details", ThemeStyle(theme.Muted)));
            }
            else
            {
                if (todo.Notes.Length > 0)
                {
                    lines.Add(new Text(string.Empty));
                    lines.Add(new Text("Notes", ThemeStyle(theme.Heading, Decoration.Bold)));
                    lines.AddRange(todo.Notes.Select(note => new Text($"• {note.Text}", ThemeStyle(theme.Text))));
                }

                if (todo.Subtasks.Length > 0)
                {
                    lines.Add(new Text(string.Empty));
                    lines.Add(new Text("Subtasks", ThemeStyle(theme.Heading, Decoration.Bold)));
                    lines.AddRange(todo.Subtasks.Select(subtask => DetailedTodoLine(subtask, 0, false, theme)));
                }
            }
        }

        return lines;
    }

    private static IRenderable CreateContent(IReadOnlyList<IRenderable> lines)
    {
        return lines.Count == 0 ? new Text(string.Empty) : new Rows(lines);
    }

    private static int AvailableContentHeight(int terminalHeight, int statusLineCount)
    {
        const int tabTableStatusBorderAndCursorHeight = 8;
        return Math.Max(1, terminalHeight - tabTableStatusBorderAndCursorHeight - statusLineCount);
    }

    private static IReadOnlyList<IRenderable> FitLines(
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

    private static IReadOnlyList<IRenderable> FitTodoLines(
        BrowserView view,
        int contentWidth,
        int contentHeight,
        TuiTheme theme)
    {
        var groups = TodoLineGroups(view, contentWidth, theme);
        if (groups.Sum(group => group.Lines.Count) <= contentHeight)
        {
            return groups.SelectMany(group => group.Lines).ToArray();
        }

        var selectedIndex = 0;
        for (var index = 0; index < groups.Count; index++)
        {
            if (groups[index].IsSelected)
            {
                selectedIndex = index;
                break;
            }
        }

        var selected = groups[selectedIndex];
        if (selected.Lines.Count > contentHeight)
        {
            return selected.Lines.Take(contentHeight).ToArray();
        }

        var start = selectedIndex;
        var end = selectedIndex;
        var usedHeight = selected.Lines.Count;
        while (start > 0 && usedHeight + groups[start - 1].Lines.Count <= contentHeight)
        {
            start--;
            usedHeight += groups[start].Lines.Count;
        }

        while (end + 1 < groups.Count && usedHeight + groups[end + 1].Lines.Count <= contentHeight)
        {
            end++;
            usedHeight += groups[end].Lines.Count;
        }

        return groups.Skip(start).Take(end - start + 1).SelectMany(group => group.Lines).ToArray();
    }

    private static int SelectedProjectIndex(BrowserView view)
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

    private static void PadToContentHeight(Table table, int contentHeight, params int[] paneLineCounts)
    {
        var renderedContentHeight = Math.Max(1, paneLineCounts.Max());

        for (var row = renderedContentHeight; row < contentHeight; row++)
        {
            table.AddEmptyRow();
        }
    }

    private static IRenderable TodoListRow(TodoRow row, int contentWidth, TuiTheme theme)
    {
        var todo = row.Todo!;
        var cursor = row.IsSelected ? ">" : " ";
        var indent = new string(' ', row.Depth * 2);
        var status = todo.IsCompleted ? "[x]" : "[ ]";
        var priority = PriorityMarker(todo.Priority);
        var prefix = $"{cursor} {indent}{status}{priority} ";
        var titleWidth = Math.Max(1, contentWidth - DisplayWidth(prefix));
        var title = Truncate(todo.Title, titleWidth);
        var line = new System.Text.StringBuilder();
        AppendStyled(
            line,
            cursor,
            row.IsSelected ? theme.Accent : todo.IsCompleted ? theme.Success : theme.Text,
            row.IsSelected ? Decoration.Bold : todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(
            line,
            $" {indent}",
            todo.IsCompleted ? theme.Success : theme.Text,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(
            line,
            status,
            todo.IsCompleted ? theme.Success : theme.Accent,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(line, priority, PriorityColor(todo.Priority, theme));
        AppendStyled(
            line,
            $" {title}",
            todo.IsCompleted ? theme.Success : theme.Text,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        return new Markup(line.ToString());
    }

    private static IRenderable TodoScheduleLine(TodoRow row, int contentWidth, TuiTheme theme)
    {
        var schedule = row.Todo!.Schedule!;
        var titleIndentWidth = Math.Min(TodoTitleStartWidth(row), Math.Max(0, contentWidth - 1));
        var availableWidth = Math.Max(1, contentWidth - titleIndentWidth);
        var value = Truncate($"⏳ {schedule.Date:yyyy-MM-dd} {schedule.Time:HH:mm}", availableWidth);
        var line = new System.Text.StringBuilder();
        AppendStyled(line, new string(' ', titleIndentWidth), theme.Text);
        AppendStyled(line, value, theme.Date, Decoration.Dim);
        return new Markup(line.ToString());
    }

    private static int TodoTitleStartWidth(TodoRow row)
    {
        var indent = new string(' ', row.Depth * 2);
        var status = row.Todo!.IsCompleted ? "[x]" : "[ ]";
        return DisplayWidth($"  {indent}{status}{PriorityMarker(row.Todo.Priority)} ");
    }

    private static string Truncate(string value, int width)
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

    private static int DisplayWidth(string value) => value.GetCellWidth();

    private static IRenderable DetailedTodoLine(TodoItem todo, int depth, bool selected, TuiTheme theme)
    {
        var cursor = selected ? ">" : " ";
        var indent = new string(' ', depth * 2);
        var status = todo.IsCompleted ? "[x]" : "[ ]";
        var reference = todo.ExternalReference is null ? string.Empty : $"{todo.ExternalReference} - ";
        var priority = PriorityMarker(todo.Priority);
        var tags = todo.Tags.Length == 0 ? string.Empty : $" {string.Join(' ', todo.Tags.Select(tag => $"#{tag}"))}";
        var start = todo.StartDate is null ? string.Empty : $" 🛫 {todo.StartDate:yyyy-MM-dd}";
        var due = todo.DueDate is null ? string.Empty : $" 📅 {todo.DueDate:yyyy-MM-dd}";

        var line = new System.Text.StringBuilder();
        AppendStyled(
            line,
            cursor,
            selected ? theme.Accent : todo.IsCompleted ? theme.Success : theme.Text,
            selected ? Decoration.Bold : todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(
            line,
            $" {indent}",
            todo.IsCompleted ? theme.Success : theme.Text,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(
            line,
            status,
            todo.IsCompleted ? theme.Success : theme.Accent,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(line, priority, PriorityColor(todo.Priority, theme));
        AppendStyled(
            line,
            $" {reference}{todo.Title}",
            todo.IsCompleted ? theme.Success : theme.Text,
            todo.IsCompleted ? Decoration.Dim : Decoration.None);
        AppendStyled(line, tags, theme.Tag);
        AppendStyled(line, start + due, theme.Date);
        return new Markup(line.ToString());
    }

    private static string PriorityMarker(TodoPriority? priority) => priority switch
    {
        TodoPriority.Highest => " 🔺",
        TodoPriority.High => " ⏫",
        TodoPriority.Medium => " 🔼",
        TodoPriority.Low => " 🔽",
        TodoPriority.Lowest => " ⏬",
        _ => string.Empty
    };

    private static Color PriorityColor(TodoPriority? priority, TuiTheme theme) => priority switch
    {
        TodoPriority.Highest => theme.Error,
        TodoPriority.High => theme.Warning,
        TodoPriority.Medium => theme.Accent,
        TodoPriority.Low => theme.Muted,
        TodoPriority.Lowest => theme.Muted,
        _ => theme.Text
    };

    private static void AddField(
        List<IRenderable> lines,
        string name,
        string? value,
        TuiTheme theme,
        Color valueColor)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var line = new System.Text.StringBuilder();
            AppendStyled(line, $"{name}: ", theme.Heading, Decoration.Bold);
            AppendStyled(line, value, valueColor);
            lines.Add(new Markup(line.ToString()));
        }
    }

    private static IReadOnlyList<BrowserStatusLine> CreateStatusLines(
        BrowserView view,
        TuiKeyBindings keyBindings,
        bool compact,
        int terminalWidth,
        int terminalHeight)
    {
        if (view.CommandPalette is not null)
        {
            return DefaultStatusLines(CommandPaletteStatus(
                view.CommandPalette,
                keyBindings,
                terminalWidth,
                terminalHeight));
        }

        if (view.GlobalCommand is not null)
        {
            return [new BrowserStatusLine(view.GlobalCommand)];
        }

        if (view.GlobalError is not null)
        {
            return DefaultStatusLines(WrapStatus(view.GlobalError, Math.Max(1, terminalWidth - 4)));
        }

        if (view.State.Form is not null)
        {
            return TodoFormStatus(view, keyBindings, terminalWidth, terminalHeight);
        }

        if (view.State.ContentEditor is not null)
        {
            return DefaultStatusLines(TodoContentEditorStatus(
                view.State.ContentEditor,
                keyBindings,
                terminalWidth,
                terminalHeight));
        }

        if (view.State.IsSortMode)
        {
            string[] menuLines = terminalWidth switch
            {
                >= 100 =>
                    ["Sort: n/N name  d/D start  p/P priority  t/T tags  f/F file  o source  Esc cancel"],
                >= 60 =>
                [
                    "Sort: n/N name  d/D start  p/P priority",
                    "t/T tags  f/F file  o source  Esc cancel"
                ],
                _ =>
                [
                    "Sort: n/N name  d/D start",
                    "p/P priority  t/T tags",
                    "f/F file  o source",
                    "Esc cancel"
                ]
            };

            return DefaultStatusLines(menuLines
                .SelectMany(line => WrapStatus(line, Math.Max(1, terminalWidth - 4)))
                .ToArray());
        }

        var status = view.State switch
        {
            { IsFilterMode: true } => $"/{view.State.FilterDraft}",
            { Error: not null } => view.State.Error,
            { FilterText.Length: > 0 } =>
                $"Filter: /{view.State.FilterText}  {Shortest(keyBindings.FilterMode)} edit  " +
                $"empty Enter clears  {SortHint(view.State, keyBindings)}",
            _ when compact => CompactStatus(keyBindings, view.State),
            _ => NormalStatus(keyBindings, view.State)
        };

        return DefaultStatusLines(WrapStatus(status, Math.Max(1, terminalWidth - 4)));
    }

    private static IReadOnlyList<BrowserStatusLine> TodoFormStatus(
        BrowserView view,
        TuiKeyBindings bindings,
        int terminalWidth,
        int terminalHeight)
    {
        var form = view.State.Form!;
        if (form.IsChoosingProject)
        {
            var projects = view.Projects.Where(project => project.Project is not null).ToArray();
            var title = projects.Length == 0
                ? "No valid projects"
                : projects[Math.Clamp(form.ProjectPickerIndex, 0, projects.Length - 1)].Title;
            return DefaultStatusLines(WrapStatus(
                $"Choose project: {title}  {Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} move  " +
                $"{Shortest(bindings.Open)} select  {Shortest(bindings.Back)} cancel",
                Math.Max(1, terminalWidth - 4)));
        }

        var fields = new[]
        {
            (TodoFormField.Title, "Title", form.Values.Title),
            (TodoFormField.Reference, "External reference", form.Values.ExternalReference ?? string.Empty),
            (TodoFormField.Priority, "Priority", form.Values.Priority?.ToString() ?? string.Empty),
            (TodoFormField.Tags, "Tags", string.Join(' ', form.Values.Tags.Select(tag => $"#{tag}"))),
            (TodoFormField.StartDate, "Start date", form.Values.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty),
            (TodoFormField.DueDate, "Due date", form.Values.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty)
        };
        var contentWidth = Math.Max(1, terminalWidth - 4);
        var selectedIndex = Array.FindIndex(fields, field => field.Item1 == form.Field);
        var lines = new List<BrowserStatusLine>();
        if (terminalHeight >= 24)
        {
            foreach (var field in fields)
            {
                lines.Add(new BrowserStatusLine(field.Item2, BrowserStatusRole.FormLabel));
                lines.Add(FormValueLine(form, field.Item1, field.Item3, contentWidth));
            }
        }
        else
        {
            var field = fields[Math.Max(0, selectedIndex)];
            lines.Add(new BrowserStatusLine(
                $"{field.Item2} ({Math.Max(0, selectedIndex) + 1}/{fields.Length})",
                BrowserStatusRole.FormLabel));
            lines.Add(FormValueLine(form, field.Item1, field.Item3, contentWidth));
        }

        var message = form.Error ??
            $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} field  " +
            $"{Shortest(bindings.Open)} edit  {Shortest(bindings.SaveForm)} save  " +
            $"{Shortest(bindings.Back)} cancel";
        var messageRole = form.Error is null
            ? BrowserStatusRole.FormHint
            : BrowserStatusRole.FormError;
        lines.AddRange(WrapStatus(message, contentWidth)
            .Select(line => new BrowserStatusLine(line, messageRole)));
        return lines;
    }

    private static BrowserStatusLine FormValueLine(
        TodoFormState form,
        TodoFormField field,
        string value,
        int contentWidth)
    {
        var selected = field == form.Field;
        var prefix = selected ? "> " : "  ";
        var availableWidth = Math.Max(1, contentWidth - prefix.Length);
        if (form.IsEditing && selected)
        {
            var display = availableWidth == 1
                ? "_"
                : Truncate(form.Draft, availableWidth - 1) + "_";
            return new BrowserStatusLine(prefix + display, BrowserStatusRole.FormActiveValue);
        }

        var valueOrPlaceholder = string.IsNullOrEmpty(value) ? "—" : value;
        var role = selected
            ? BrowserStatusRole.FormActiveValue
            : string.IsNullOrEmpty(value)
                ? BrowserStatusRole.FormPlaceholder
                : BrowserStatusRole.FormValue;
        return new BrowserStatusLine(prefix + Truncate(valueOrPlaceholder, availableWidth), role);
    }

    private static IReadOnlyList<string> TodoContentEditorStatus(
        TodoContentEditorState editor,
        TuiKeyBindings bindings,
        int terminalWidth,
        int terminalHeight)
    {
        var width = Math.Max(1, terminalWidth - 4);
        if (editor.Mode == ContentEditorMode.Edit)
        {
            var kind = editor.Focus == ContentEditorFocus.Notes ? "note" : "subtask";
            return WrapStatus(
                $"{(editor.IsAdding ? "Add" : "Edit")} {kind}: {editor.Draft}_  Enter accept  Esc cancel",
                width);
        }

        if (editor.Mode == ContentEditorMode.ConfirmRemoval)
        {
            var selected = editor.Subtasks[editor.SubtaskIndex];
            return WrapStatus(
                $"Remove '{selected.Title}' and {selected.DescendantCount} nested item(s)?  " +
                $"{Shortest(bindings.Open)} confirm  {Shortest(bindings.Back)} cancel",
                width);
        }

        var lines = new List<string> { $"Content: {editor.TodoTitle}" };
        var rowsPerSection = Math.Max(1, Math.Min(3, (terminalHeight - 12) / 2));
        AddContentRows(
            lines,
            "Notes",
            editor.Notes.Select(note => note.Text).ToArray(),
            editor.NoteIndex,
            editor.Focus == ContentEditorFocus.Notes,
            rowsPerSection);
        AddContentRows(
            lines,
            "Subtasks",
            editor.Subtasks.Select(todo => $"[{(todo.IsCompleted ? 'x' : ' ')}] {todo.Title}").ToArray(),
            editor.SubtaskIndex,
            editor.Focus == ContentEditorFocus.Subtasks,
            rowsPerSection);
        lines.Add(editor.Error ??
            $"{Shortest(bindings.FocusNext)} section  {Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} move  " +
            $"{Shortest(bindings.CreateTodo)} add  {Shortest(bindings.EditTodo)} edit  " +
            $"{Shortest(bindings.RemoveContent)} remove  {Shortest(bindings.SaveForm)} save  " +
            $"{Shortest(bindings.Back)} cancel");
        return lines.SelectMany(line => WrapStatus(line, width)).ToArray();
    }

    private static IReadOnlyList<string> CommandPaletteStatus(
        CommandPaletteView palette,
        TuiKeyBindings bindings,
        int terminalWidth,
        int terminalHeight)
    {
        var width = Math.Max(1, terminalWidth - 4);
        var visibleRows = Math.Max(1, Math.Min(7, terminalHeight - 13));
        var start = Math.Clamp(
            palette.SelectedIndex - visibleRows + 1,
            0,
            Math.Max(0, palette.Items.Length - visibleRows));
        var lines = new List<string>
        {
            palette.State.IsSearching ? $"Command palette  /{palette.State.Query}_" : "Command palette"
        };
        if (palette.Items.Length == 0)
        {
            lines.Add("  No matching actions");
        }
        else
        {
            for (var index = start; index < Math.Min(palette.Items.Length, start + visibleRows); index++)
            {
                var item = palette.Items[index];
                var marker = index == palette.SelectedIndex ? ">" : " ";
                var unavailable = item.IsEnabled ? string.Empty : $" — {item.DisabledReason}";
                lines.Add($"{marker} {item.Group}: {item.Label}  [{item.Binding}]{unavailable}");
            }
        }

        lines.Add(palette.State.Error ??
            $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} move  " +
            $"{Shortest(bindings.FilterMode)} search  {Shortest(bindings.Open)} run  " +
            $"{Shortest(bindings.Back)} close");
        return lines.SelectMany(line => WrapStatus(line, width)).ToArray();
    }

    private static void AddContentRows(
        List<string> output,
        string heading,
        IReadOnlyList<string> values,
        int selectedIndex,
        bool focused,
        int visibleRows)
    {
        output.Add($"{(focused ? ">" : " ")} {heading}");
        if (values.Count == 0)
        {
            output.Add("    —");
            return;
        }

        var start = Math.Clamp(selectedIndex - visibleRows + 1, 0, Math.Max(0, values.Count - visibleRows));
        for (var index = start; index < Math.Min(values.Count, start + visibleRows); index++)
        {
            output.Add($"  {(focused && index == selectedIndex ? ">" : " ")} {values[index]}");
        }
    }

    private static IReadOnlyList<string> WrapStatus(string value, int width)
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

    private static IReadOnlyList<BrowserStatusLine> DefaultStatusLines(IEnumerable<string> lines) =>
        lines.Select(line => new BrowserStatusLine(line)).ToArray();

    private static void WriteStatus(IReadOnlyList<BrowserStatusLine> lines, BrowserView view, TuiTheme theme)
    {
        var defaultStyle = view.State switch
        {
            _ when view.GlobalError is not null || view.CommandPalette?.State.Error is not null =>
                ThemeStyle(theme.Error, Decoration.Bold),
            _ when view.GlobalCommand is not null || view.CommandPalette is not null => ThemeStyle(theme.Accent),
            { Error: not null } => ThemeStyle(theme.Error, Decoration.Bold),
            { IsFilterMode: true } => ThemeStyle(theme.Accent),
            { IsSortMode: true } => ThemeStyle(theme.Accent),
            { Form: not null } => ThemeStyle(theme.Accent),
            { ContentEditor: not null } => ThemeStyle(theme.Accent),
            _ => ThemeStyle(theme.Muted, Decoration.Dim)
        };
        var content = lines.Select(line => new Text(
            line.Text,
            line.Role switch
            {
                BrowserStatusRole.FormLabel => ThemeStyle(theme.Heading, Decoration.Bold),
                BrowserStatusRole.FormValue => ThemeStyle(theme.Text),
                BrowserStatusRole.FormActiveValue => ThemeStyle(theme.Accent, Decoration.Bold),
                BrowserStatusRole.FormPlaceholder => ThemeStyle(theme.Muted, Decoration.Dim),
                BrowserStatusRole.FormHint => ThemeStyle(theme.Muted, Decoration.Dim),
                BrowserStatusRole.FormError => ThemeStyle(theme.Error, Decoration.Bold),
                _ => defaultStyle
            }));
        AnsiConsole.Write(new Panel(new Rows(content))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeStyle(theme.Border),
            Expand = true
        });
    }

    private static string NormalStatus(TuiKeyBindings bindings, BrowserState state) =>
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} navigate  " +
        $"{Shortest(bindings.FocusNext)} pane  {Shortest(bindings.Open)} open  {Shortest(bindings.Back)} back  " +
        $"{Shortest(bindings.FilterMode)} filter  {Shortest(bindings.CommandMode)} command  " +
        $"{Shortest(bindings.ToggleDetails)} details  " +
        $"{bindings.ToggleCompletedCommand}  {bindings.QuitCommand}  {SortHint(state, bindings)}";

    private static string CompactStatus(TuiKeyBindings bindings, BrowserState state) =>
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} move  " +
        $"{Shortest(bindings.Back)}/{Shortest(bindings.Open)} back/open  " +
        $"{Shortest(bindings.FilterMode)} filter  {Shortest(bindings.ToggleDetails)} details  " +
        $"{Shortest(bindings.CommandMode)} commands  " +
        SortHint(state, bindings);

    private static string SortHint(BrowserState state, TuiKeyBindings bindings)
    {
        var launcher = Shortest(bindings.SortMode);
        if (state.Sort.Property == TodoSortProperty.Source)
        {
            return $"{launcher} sort";
        }

        var property = state.Sort.Property switch
        {
            TodoSortProperty.Name => "name",
            TodoSortProperty.StartDate => "start",
            TodoSortProperty.Tags => "tags",
            TodoSortProperty.File => "file",
            TodoSortProperty.Priority => "priority",
            _ => "source"
        };
        var direction = state.Sort.Direction == TodoSortDirection.Ascending ? "↑" : "↓";
        return $"{launcher} {property}{direction}";
    }

    private static string Shortest(System.Collections.Immutable.ImmutableArray<KeyGesture> gestures) =>
        TuiKeyBindings.ShortestDisplayName(gestures);

    private static Style ThemeStyle(Color color, Decoration decoration = Decoration.None) =>
        new(color, decoration: decoration);

    private static void AppendStyled(
        System.Text.StringBuilder output,
        string value,
        Color color,
        Decoration decoration = Decoration.None)
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

    private static int SafeWindowWidth()
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

    private static int SafeWindowHeight()
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

    private static int LongestLine(string content) => content
        .Split(Environment.NewLine, StringSplitOptions.None)
        .Max(line => line.Length);

    private sealed record TodoLineGroup(IReadOnlyList<IRenderable> Lines, bool IsSelected);

    private sealed record BrowserStatusLine(
        string Text,
        BrowserStatusRole Role = BrowserStatusRole.Default);

    private enum BrowserStatusRole
    {
        Default,
        FormLabel,
        FormValue,
        FormActiveValue,
        FormPlaceholder,
        FormHint,
        FormError
    }
}
