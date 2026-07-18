using System.Collections.Immutable;
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
    private readonly Func<DateOnly> todayProvider;
    private bool browserRendered;

    public SpectreTerminalUi() : this(SafeWindowWidth, SafeWindowHeight, null)
    {
    }

    public SpectreTerminalUi(
        Func<int> widthProvider,
        Func<int> heightProvider,
        Func<DateOnly>? todayProvider = null)
    {
        this.widthProvider = widthProvider;
        this.heightProvider = heightProvider;
        this.todayProvider = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today));
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
        var today = todayProvider();
        var statusLines = CreateStatusLines(view, keyBindings, compact, width, height);
        var contentHeight = AvailableContentHeight(height, statusLines.Count);
        WriteOperationalHeader(
            tabs,
            keyBindings,
            theme,
            width,
            BrowserMode(view),
            today,
            view.Projects.FirstOrDefault()?.ActiveCount ?? 0,
            view.Projects.Count(project => project.Error is not null));

        if (width >= 120 && height >= 24)
        {
            WriteWide(view, width, contentHeight, theme, today);
        }
        else if (width >= 80 && height >= 18)
        {
            WriteMedium(view, width, contentHeight, theme, today);
        }
        else
        {
            WriteNarrow(view, width, contentHeight, theme, today);
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
        WriteOperationalHeader(
            tabs,
            keyBindings,
            theme,
            width,
            PlannerModeLabel(view),
            view.State.SelectedDate,
            view.OpenTodoCount,
            view.ProjectErrorCount);
        var status = PlannerStatus(view, keyBindings, width, height);
        var pickerVisible = view.State.Mode is PlannerMode.ChooseTodo or PlannerMode.EditFilter;
        var pickerRows = pickerVisible ? Math.Clamp(height / 5, 3, 7) : 0;
        var wideDetails = view.State.ShowDetails && width >= 120;
        var compactDetails = view.State.ShowDetails && !wideDetails &&
                             view.State.Mode == PlannerMode.Browse &&
                             view.State.Form is null &&
                             view.State.ContentEditor is null &&
                             view.CommandPalette is null &&
                             view.GlobalCommand is null;
        const int tabTableStatusBorderAndCursorHeight = 8;
        var pickerHeight = pickerVisible ? pickerRows + 2 : 0;
        const int compactDetailsHeight = 3;
        var reservedHeight = tabTableStatusBorderAndCursorHeight + pickerHeight +
                             (compactDetails ? compactDetailsHeight : 0);
        var availableRows = Math.Max(1, height - status.Count - reservedHeight);
        var visibleSlots = WindowPlannerSlots(view.Slots, view.State.SlotIndex, availableRows);
        var table = new Table().SquareBorder().Expand();
        table.BorderStyle = ThemeStyle(theme.Border);
        table.AddColumn(new TableColumn(new Text(
            view.State.SelectedDate.ToString("ddd yyyy-MM-dd"),
            ThemeStyle(theme.Heading, Decoration.Bold)))
        {
            Width = 14,
            NoWrap = true
        });
        table.AddColumn(new TableColumn(new Text("PLAN", ThemeStyle(theme.Accent, Decoration.Bold))));
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
                var prefix = slot.IsSelected ? ">" : " ";
                var state = assignment.Todo.IsCompleted ? "✓" : "○";
                var priority = PriorityCode(assignment.Todo.Priority);
                content = new Text(
                    $"{prefix} {state} {priority} {assignment.Todo.Title}  [{assignment.ProjectTitle}]",
                    ThemeStyle(
                        assignment.Todo.IsCompleted ? theme.Muted : slot.IsSelected ? theme.Accent : theme.Text,
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

        if (wideDetails)
        {
            var timelineWidth = Math.Max(40, (width * 2 / 3) - 2);
            var shell = new Table().NoBorder().Collapse().HideHeaders();
            shell.AddColumn(new TableColumn(string.Empty).Width(timelineWidth).NoWrap());
            shell.AddColumn(new TableColumn(string.Empty).NoWrap());
            shell.AddRow(
                table,
                new Panel(CreateContent(FitLines(
                    PlannerDetailLines(view, theme),
                    Math.Max(1, availableRows + 1),
                    0)))
                {
                    Header = new PanelHeader("INSPECTOR"),
                    Border = BoxBorder.Square,
                    BorderStyle = ThemeStyle(theme.Border),
                    Expand = true
                });
            AnsiConsole.Write(shell);
        }
        else
        {
            AnsiConsole.Write(table);
            if (compactDetails)
            {
                AnsiConsole.Write(new Panel(PlannerCompactDetail(view, theme))
                {
                    Header = new PanelHeader("SELECTED"),
                    Border = BoxBorder.Square,
                    BorderStyle = ThemeStyle(theme.Border),
                    Expand = true
                });
            }
        }

        if (view.State.Mode is PlannerMode.ChooseTodo or PlannerMode.EditFilter)
        {
            WritePlannerPicker(view, theme, width, pickerRows);
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

    private static IReadOnlyList<BrowserStatusLine> PlannerStatus(
        PlannerView view,
        TuiKeyBindings bindings,
        int terminalWidth,
        int terminalHeight)
    {
        IReadOnlyList<string> status;
        if (view.CommandPalette is not null)
        {
            return DefaultStatusLines(
                CommandPaletteStatus(view.CommandPalette, bindings, terminalWidth, terminalHeight));
        }

        if (view.State.Form is not null)
        {
            return TodoFormStatus(
                view.State.Form,
                view.Projects
                    .Select(project => new TodoEditorProjectOption(project.Title, project.Path))
                    .ToArray(),
                bindings,
                terminalWidth,
                terminalHeight);
        }

        if (view.State.ContentEditor is not null)
        {
            return DefaultStatusLines(TodoContentEditorStatus(
                view.State.ContentEditor,
                bindings,
                terminalWidth,
                terminalHeight));
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
                    $"CHOOSE TODO  {Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
                    $"{Shortest(bindings.Open)} ASSIGN  {Shortest(bindings.FilterMode)} FILTER  " +
                    $"{Shortest(bindings.Back)} CANCEL"
                ],
                PlannerMode.MoveTodo =>
                [
                    $"MOVE TODO  {Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} SLOT  " +
                    $"{Shortest(bindings.PlannerPreviousDay)}/{Shortest(bindings.PlannerNextDay)} DAY  " +
                    $"{Shortest(bindings.Open)} PLACE  {Shortest(bindings.Back)} CANCEL"
                ],
                _ =>
                [
                    $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} SLOT  " +
                    $"{Shortest(bindings.PlannerPreviousDay)}/{Shortest(bindings.PlannerNextDay)} DAY  " +
                    $"{Shortest(bindings.PlannerToday)} TODAY  {Shortest(bindings.Open)} ASSIGN/MOVE  " +
                    $"{Shortest(bindings.PlannerUnschedule)} UNSCHEDULE  " +
                    $"{Shortest(bindings.CreateTodo)} CREATE  {Shortest(bindings.EditTodo)} EDIT  " +
                    $"{Shortest(bindings.ToggleTodo)} COMPLETE  {Shortest(bindings.ToggleDetails)} DETAILS"
                ]
            };
        }

        var statusWidth = Math.Max(1, terminalWidth - 4);
        return DefaultStatusLines(status.SelectMany(line => WrapStatus(line, statusWidth)));
    }

    private static void WritePlannerPicker(PlannerView view, TuiTheme theme, int width, int visibleRows)
    {
        var lines = new List<IRenderable>();
        if (view.PickerTodos.Length == 0)
        {
            lines.Add(new Text("No open unscheduled todos", ThemeStyle(theme.Muted, Decoration.Dim)));
        }
        else
        {
            var start = Math.Clamp(
                view.State.PickerIndex - visibleRows + 1,
                0,
                Math.Max(0, view.PickerTodos.Length - visibleRows));
            foreach (var (todo, index) in view.PickerTodos
                         .Skip(start)
                         .Take(visibleRows)
                         .Select((todo, index) => (todo, index + start)))
            {
                var selected = index == view.State.PickerIndex;
                lines.Add(new Text(
                    $"{(selected ? ">" : " ")} {todo.Todo.Title}  [{todo.ProjectTitle}]",
                    ThemeStyle(
                        selected ? theme.Accent : theme.Text,
                        selected ? Decoration.Bold : Decoration.None)).Ellipsis());
            }
        }

        while (lines.Count < visibleRows)
        {
            lines.Add(new Text(string.Empty));
        }

        AnsiConsole.Write(new Panel(new Rows(lines))
        {
            Header = new PanelHeader("UNSCHEDULED TODOS"),
            Border = BoxBorder.Square,
            BorderStyle = ThemeStyle(theme.Border),
            Expand = true
        });
    }

    private static void WritePlannerStatus(
        IReadOnlyList<BrowserStatusLine> lines,
        PlannerView view,
        TuiTheme theme)
    {
        var defaultStyle = view.GlobalError is not null || view.State.Error is not null ||
                    view.CommandPalette?.State.Error is not null
            ? ThemeStyle(theme.Error, Decoration.Bold)
            : view.GlobalCommand is not null || view.CommandPalette is not null
                ? ThemeStyle(theme.Accent)
            : view.State.Mode == PlannerMode.Browse
                ? ThemeStyle(theme.Muted, Decoration.Dim)
                : ThemeStyle(theme.Accent);
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
            Border = BoxBorder.Square,
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

    private static void WriteWide(
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
            AnsiConsole.Write(twoPaneTable);
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
            CreateContent(detailLines));
        PadToContentHeight(table, contentHeight, projectLines.Count, todoLines.Count, detailLines.Count);
        AnsiConsole.Write(table);
    }

    private static void WriteMedium(
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
            AnsiConsole.Write(navigation);
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
            AnsiConsole.Write(tasks);
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
        table.AddRow(CreateContent(todos), CreateContent(details));
        PadToContentHeight(table, contentHeight, todos.Count, details.Count);
        AnsiConsole.Write(table);
    }

    private static void WriteNarrow(
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
        table.AddRow(CreateContent(lines));
        PadToContentHeight(table, contentHeight, lines.Count);

        AnsiConsole.Write(table);
    }

    private static Table CreatePaneTable(
        TuiTheme theme,
        params (string Title, int? Width, bool Focused, bool NoWrap)[] panes)
    {
        var table = new Table().SquareBorder().Expand();
        table.BorderStyle = ThemeStyle(theme.Border);

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

    private static void WriteOperationalHeader(
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

        segments.Add(($"  MODE:{mode}", theme.Text, Decoration.None));
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

        AnsiConsole.Write(new Markup(output.ToString()));
        AnsiConsole.WriteLine();
    }

    private static string BrowserMode(BrowserView view) => view switch
    {
        { CommandPalette: not null } => "HELP",
        { GlobalCommand: not null } => "COMMAND",
        { GlobalError: not null } => "ERROR",
        { State.ContentEditor: not null } => "CONTENT",
        { State.Form.IsCreate: true } => "CREATE",
        { State.Form: not null } => "EDIT",
        { State.IsFilterMode: true } => "FILTER",
        { State.IsSortMode: true } => "SORT",
        { State.Error: not null } => "ERROR",
        _ => "BROWSE"
    };

    private static string PlannerModeLabel(PlannerView view) => view switch
    {
        { CommandPalette: not null } => "HELP",
        { GlobalCommand: not null } => "COMMAND",
        { GlobalError: not null } => "ERROR",
        { State.ContentEditor: not null } => "CONTENT",
        { State.Form.IsCreate: true } => "CREATE",
        { State.Form: not null } => "EDIT",
        { State.Mode: PlannerMode.EditFilter } => "FILTER",
        { State.Mode: PlannerMode.ChooseTodo } => "PICK",
        { State.Mode: PlannerMode.MoveTodo } => "MOVE",
        { State.Error: not null } => "ERROR",
        _ => "BROWSE"
    };

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

        var layout = TodoColumns(contentWidth, view.SelectedProjectPath is null);
        var groups = new List<TodoLineGroup>
        {
            new([TodoColumnHeader(layout, theme)], false)
        };
        groups.AddRange(view.Todos.Select(row =>
        {
            if (row.Heading is not null)
            {
                return new TodoLineGroup(
                    [new Text(row.Heading, ThemeStyle(theme.Heading, Decoration.Bold)).Ellipsis()],
                    false);
            }

            var lines = new List<IRenderable> { TodoListRow(
                row,
                layout,
                theme) };

            return new TodoLineGroup(lines, row.IsSelected);
        }));
        return groups;
    }

    private static IReadOnlyList<IRenderable> DetailLines(BrowserView view, TuiTheme theme)
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

    private static IReadOnlyList<IRenderable> PlannerDetailLines(PlannerView view, TuiTheme theme)
    {
        if (view.SelectedSlot.Assignments.Length > 1)
        {
            return
            [
                new Text("CONFLICTING ASSIGNMENTS", ThemeStyle(theme.Error, Decoration.Bold)),
                new Text("Resolve the duplicate schedule metadata before editing this slot.",
                    ThemeStyle(theme.Error))
            ];
        }

        if (view.SelectedAssignment is null)
        {
            return [new Text("EMPTY TIMESLOT", ThemeStyle(theme.Muted, Decoration.Dim))];
        }

        var assignment = view.SelectedAssignment;
        var todo = assignment.Todo;
        var lines = new List<IRenderable>
        {
            new Text(todo.Title, ThemeStyle(theme.Heading, Decoration.Bold))
        };
        AddField(lines, "Project", assignment.ProjectTitle, theme, theme.Text);
        if (!string.IsNullOrEmpty(todo.SectionPath))
        {
            AddField(lines, "Section", todo.SectionPath, theme, theme.Text);
        }

        AddField(lines, "Reference", todo.ExternalReference, theme, theme.Text);
        AddField(lines, "Priority", todo.Priority?.ToString(), theme, PriorityColor(todo.Priority, theme));
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
                : $"{todo.Schedule.Date:yyyy-MM-dd} {todo.Schedule.Time:HH:mm}",
            theme,
            theme.Date);

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
            lines.AddRange(todo.Subtasks.Select(subtask => DetailedTodoLine(subtask, [], false, theme)));
        }

        if (todo.Notes.Length == 0 && todo.Subtasks.Length == 0)
        {
            lines.Add(new Text(string.Empty));
            lines.Add(new Text("NO ADDITIONAL DETAILS", ThemeStyle(theme.Muted, Decoration.Dim)));
        }

        return lines;
    }

    private static IRenderable PlannerCompactDetail(PlannerView view, TuiTheme theme)
    {
        if (view.SelectedSlot.Assignments.Length > 1)
        {
            return new Text(
                $"{view.SelectedSlot.Assignments.Length} conflicting assignments",
                ThemeStyle(theme.Error, Decoration.Bold)).Ellipsis();
        }

        if (view.SelectedAssignment is null)
        {
            return new Text("Empty timeslot", ThemeStyle(theme.Muted, Decoration.Dim));
        }

        var assignment = view.SelectedAssignment;
        var todo = assignment.Todo;
        var metadata = new[]
        {
            assignment.ProjectTitle,
            todo.Priority?.ToString(),
            todo.Tags.Length == 0 ? null : string.Join(' ', todo.Tags.Select(tag => $"#{tag}")),
            todo.Schedule is null
                ? null
                : $"{todo.Schedule.Date:yyyy-MM-dd} {todo.Schedule.Time:HH:mm}"
        };
        var line = new System.Text.StringBuilder();
        AppendStyled(line, todo.Title, theme.Heading, Decoration.Bold);
        AppendStyled(
            line,
            $"  {string.Join(" · ", metadata.Where(value => !string.IsNullOrEmpty(value)))}",
            theme.Muted,
            Decoration.Dim);
        return new Markup(line.ToString()).Ellipsis();
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

    private static TodoColumnLayout TodoColumns(int contentWidth, bool includeProject)
    {
        var showProject = includeProject && contentWidth >= 52;
        var showSchedule = contentWidth >= 44;
        const int projectWidth = 10;
        const int scheduleWidth = 16;
        var fixedWidth = 6 + (showProject ? projectWidth + 2 : 0) +
                         (showSchedule ? scheduleWidth + 2 : 0);
        return new TodoColumnLayout(
            contentWidth,
            Math.Max(1, contentWidth - fixedWidth),
            showProject,
            projectWidth,
            showSchedule,
            scheduleWidth);
    }

    private static IRenderable TodoColumnHeader(TodoColumnLayout layout, TuiTheme theme)
    {
        var text = $"  S P {FitColumn("TASK", layout.TaskWidth)}";
        if (layout.ShowProject)
        {
            text += $"  {FitColumn("PROJECT", layout.ProjectWidth)}";
        }

        if (layout.ShowSchedule)
        {
            text += $"  {FitColumn("SCHEDULED", layout.ScheduleWidth)}";
        }

        return new Text(Truncate(text, layout.ContentWidth), ThemeStyle(theme.Heading, Decoration.Bold));
    }

    private static IRenderable TodoListRow(
        TodoRow row,
        TodoColumnLayout layout,
        TuiTheme theme)
    {
        var todo = row.Todo!;
        var cursor = row.IsSelected ? ">" : " ";
        var treePrefix = TodoTreeFormatter.Format(row.TreePath);
        var status = todo.IsCompleted ? "✓" : "○";
        var priority = PriorityCode(todo.Priority);
        var prefixWidth = DisplayWidth(treePrefix);
        var visiblePrefix = prefixWidth >= layout.TaskWidth
            ? FitColumn(treePrefix, layout.TaskWidth)
            : treePrefix;
        var title = prefixWidth >= layout.TaskWidth
            ? string.Empty
            : FitColumn(todo.Title, layout.TaskWidth - prefixWidth);
        var selectedColor = row.IsSelected ? theme.Accent : theme.Text;
        var baseColor = todo.IsCompleted ? theme.Muted : selectedColor;
        var treeColor = row.IsSelected ? theme.Accent : theme.Muted;
        var decoration = row.IsSelected
            ? Decoration.Bold
            : todo.IsCompleted ? Decoration.Dim : Decoration.None;
        var line = new System.Text.StringBuilder();
        AppendStyled(line, $"{cursor} ", baseColor, decoration);
        AppendStyled(line, status, baseColor, decoration);
        AppendStyled(line, " ", baseColor, decoration);
        AppendStyled(
            line,
            priority,
            row.IsSelected || todo.IsCompleted ? baseColor : PriorityColor(todo.Priority, theme),
            decoration);
        AppendStyled(line, " ", baseColor, decoration);
        AppendStyled(line, visiblePrefix, treeColor, decoration);
        AppendStyled(line, title, baseColor, decoration);
        if (layout.ShowProject)
        {
            AppendStyled(
                line,
                $"  {FitColumn(row.ProjectTitle ?? "-", layout.ProjectWidth)}",
                baseColor,
                decoration);
        }

        if (layout.ShowSchedule)
        {
            var schedule = todo.Schedule is null
                ? "-"
                : $"{todo.Schedule.Date:yyyy-MM-dd} {todo.Schedule.Time:HH:mm}";
            var scheduleColor = row.IsSelected || todo.IsCompleted
                ? baseColor
                : theme.Date;
            AppendStyled(
                line,
                $"  {FitColumn(schedule, layout.ScheduleWidth)}",
                scheduleColor,
                decoration);
        }

        return new Markup(line.ToString());
    }

    private static string FitColumn(string value, int width)
    {
        var result = Truncate(value, width);
        return result + new string(' ', Math.Max(0, width - DisplayWidth(result)));
    }

    private static string PriorityCode(TodoPriority? priority) => priority switch
    {
        TodoPriority.Highest => "!",
        TodoPriority.High => "H",
        TodoPriority.Medium => "M",
        TodoPriority.Low => "L",
        TodoPriority.Lowest => ".",
        _ => "-"
    };

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

    private static IEnumerable<(TodoItem Todo, ImmutableArray<TodoTreeSegment> TreePath)> FlattenSubtasks(
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

    private static IRenderable DetailedTodoLine(
        TodoItem todo,
        ImmutableArray<TodoTreeSegment> treePath,
        bool selected,
        TuiTheme theme)
    {
        var cursor = selected ? ">" : " ";
        var treePrefix = TodoTreeFormatter.Format(treePath);
        var status = todo.IsCompleted ? "✓" : "○";
        var reference = todo.ExternalReference is null ? string.Empty : $"{todo.ExternalReference} - ";
        var priority = PriorityCode(todo.Priority);
        var tags = todo.Tags.Length == 0 ? string.Empty : $" {string.Join(' ', todo.Tags.Select(tag => $"#{tag}"))}";
        var schedule = todo.Schedule is null
            ? string.Empty
            : $" ⏳ {todo.Schedule.Date:yyyy-MM-dd} {todo.Schedule.Time:HH:mm}";

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
            AppendStyled(line, $"{name.ToUpperInvariant()}: ", theme.Heading, Decoration.Bold);
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
            return TodoFormStatus(
                view.State.Form,
                view.Projects
                    .Where(project => project.Project is not null)
                    .Select(project => new TodoEditorProjectOption(project.Title, project.Project!.Path))
                    .ToArray(),
                keyBindings,
                terminalWidth,
                terminalHeight);
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
                    ["SORT // n/N NAME  d/D SCHEDULED  p/P PRIORITY  t/T TAGS  f/F FILE  o SOURCE  Esc CANCEL"],
                >= 60 =>
                [
                    "SORT // n/N NAME  d/D SCHEDULED  p/P PRIORITY",
                    "t/T TAGS  f/F FILE  o SOURCE  Esc CANCEL"
                ],
                _ =>
                [
                    "SORT // n/N NAME  d/D SCHEDULED",
                    "p/P PRIORITY  t/T TAGS",
                    "f/F FILE  o SOURCE",
                    "Esc CANCEL"
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
                $"FILTER: /{view.State.FilterText}  {Shortest(keyBindings.FilterMode)} EDIT  " +
                $"EMPTY Enter CLEARS  {SortHint(view.State, keyBindings)}",
            _ when compact => CompactStatus(keyBindings, view.State),
            _ => NormalStatus(keyBindings, view.State)
        };

        return DefaultStatusLines(WrapStatus(status, Math.Max(1, terminalWidth - 4)));
    }

    private static IReadOnlyList<BrowserStatusLine> TodoFormStatus(
        TodoFormState form,
        IReadOnlyList<TodoEditorProjectOption> projects,
        TuiKeyBindings bindings,
        int terminalWidth,
        int terminalHeight)
    {
        if (form.IsChoosingProject)
        {
            var title = projects.Count == 0
                ? "No valid projects"
                : projects[Math.Clamp(form.ProjectPickerIndex, 0, projects.Count - 1)].Title;
            return DefaultStatusLines(WrapStatus(
                $"CHOOSE PROJECT: {title}  {Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
                $"{Shortest(bindings.Open)} SELECT  {Shortest(bindings.Back)} CANCEL",
                Math.Max(1, terminalWidth - 4)));
        }

        var fields = new[]
        {
            (TodoFormField.Title, "Title", form.Values.Title),
            (TodoFormField.Reference, "External reference", form.Values.ExternalReference ?? string.Empty),
            (TodoFormField.Priority, "Priority", form.Values.Priority?.ToString() ?? string.Empty),
            (TodoFormField.Tags, "Tags", string.Join(' ', form.Values.Tags.Select(tag => $"#{tag}"))),
            (TodoFormField.ScheduledDate, "Scheduled date", form.ScheduledDate),
            (TodoFormField.ScheduledTime, "Scheduled time", form.ScheduledTime)
        };
        var contentWidth = Math.Max(1, terminalWidth - 4);
        var selectedIndex = Array.FindIndex(fields, field => field.Item1 == form.Field);
        var lines = new List<BrowserStatusLine>();
        if (terminalHeight >= 24)
        {
            foreach (var field in fields)
            {
                lines.Add(new BrowserStatusLine(field.Item2.ToUpperInvariant(), BrowserStatusRole.FormLabel));
                lines.Add(FormValueLine(form, field.Item1, field.Item3, contentWidth));
            }
        }
        else
        {
            var field = fields[Math.Max(0, selectedIndex)];
            lines.Add(new BrowserStatusLine(
                $"{field.Item2.ToUpperInvariant()} ({Math.Max(0, selectedIndex) + 1}/{fields.Length})",
                BrowserStatusRole.FormLabel));
            lines.Add(FormValueLine(form, field.Item1, field.Item3, contentWidth));
        }

        var message = form.Error ??
            $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} FIELD  " +
            $"{Shortest(bindings.Open)} EDIT  {Shortest(bindings.SaveForm)} SAVE  " +
            $"{Shortest(bindings.Back)} CANCEL";
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
                $"{(editor.IsAdding ? "ADD" : "EDIT")} {kind.ToUpperInvariant()}: {editor.Draft}_  Enter ACCEPT  Esc CANCEL",
                width);
        }

        if (editor.Mode == ContentEditorMode.ConfirmRemoval)
        {
            var selected = editor.Subtasks[editor.SubtaskIndex];
            return WrapStatus(
                $"REMOVE '{selected.Title}' AND {selected.DescendantCount} NESTED ITEM(S)?  " +
                $"{Shortest(bindings.Open)} CONFIRM  {Shortest(bindings.Back)} CANCEL",
                width);
        }

        var lines = new List<string> { $"CONTENT // {editor.TodoTitle}" };
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
            $"{Shortest(bindings.FocusNext)} SECTION  {Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
            $"{Shortest(bindings.CreateTodo)} ADD  {Shortest(bindings.EditTodo)} EDIT  " +
            $"{Shortest(bindings.RemoveContent)} REMOVE  {Shortest(bindings.SaveForm)} SAVE  " +
            $"{Shortest(bindings.Back)} CANCEL");
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
            palette.State.IsSearching ? $"COMMAND PALETTE // /{palette.State.Query}_" : "COMMAND PALETTE"
        };
        if (palette.Items.Length == 0)
        {
            lines.Add("  NO MATCHING ACTIONS");
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
            $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
            $"{Shortest(bindings.FilterMode)} SEARCH  {Shortest(bindings.Open)} RUN  " +
            $"{Shortest(bindings.Back)} CLOSE");
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
        output.Add($"{(focused ? ">" : " ")} {heading.ToUpperInvariant()}");
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
            Border = BoxBorder.Square,
            BorderStyle = ThemeStyle(theme.Border),
            Expand = true
        });
    }

    private static string NormalStatus(TuiKeyBindings bindings, BrowserState state) =>
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} NAVIGATE  " +
        $"{Shortest(bindings.FocusNext)} PANE  {Shortest(bindings.Open)} OPEN  {Shortest(bindings.Back)} BACK  " +
        $"{Shortest(bindings.FilterMode)} FILTER  {Shortest(bindings.CommandMode)} COMMAND  " +
        $"{Shortest(bindings.ToggleDetails)} DETAILS  " +
        $"{bindings.ToggleCompletedCommand}  {bindings.QuitCommand}  {SortHint(state, bindings)}";

    private static string CompactStatus(TuiKeyBindings bindings, BrowserState state) =>
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
        $"{Shortest(bindings.Back)}/{Shortest(bindings.Open)} BACK/OPEN  " +
        $"{Shortest(bindings.FilterMode)} FILTER  {Shortest(bindings.ToggleDetails)} DETAILS  " +
        $"{Shortest(bindings.CommandMode)} COMMANDS  " +
        SortHint(state, bindings);

    private static string SortHint(BrowserState state, TuiKeyBindings bindings)
    {
        var launcher = Shortest(bindings.SortMode);
        if (state.Sort.Property == TodoSortProperty.Source)
        {
            return $"{launcher} SORT";
        }

        var property = state.Sort.Property switch
        {
            TodoSortProperty.Name => "name",
            TodoSortProperty.Schedule => "scheduled",
            TodoSortProperty.Tags => "tags",
            TodoSortProperty.File => "file",
            TodoSortProperty.Priority => "priority",
            _ => "source"
        };
        var direction = state.Sort.Direction == TodoSortDirection.Ascending ? "↑" : "↓";
        return $"{launcher} {property.ToUpperInvariant()}{direction}";
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

    private sealed record TodoColumnLayout(
        int ContentWidth,
        int TaskWidth,
        bool ShowProject,
        int ProjectWidth,
        bool ShowSchedule,
        int ScheduleWidth);

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
