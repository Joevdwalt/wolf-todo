using Spectre.Console;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.ApplicationShell.Rendering;

public sealed class StatusRenderer
{
    private readonly SurfaceThemeRenderer themeRenderer = new();

    public IReadOnlyList<BrowserStatusLine> BrowserStatus(
        BrowserView view,
        TuiKeyBindings keyBindings,
        bool compact,
        int terminalWidth,
        int terminalHeight)
    {
        if (view.CommandPalette is not null)
        {
            return WithTimer(DefaultStatusLines([CommandPaletteFooter(keyBindings)]), view.TimerStatus, view.TimerIsBright, view.PomodoroCompletion);
        }

        if (view.GlobalCommand is not null)
        {
            return WithTimer([new BrowserStatusLine(view.GlobalCommand)], view.TimerStatus, view.TimerIsBright, view.PomodoroCompletion);
        }

        if (view.GlobalError is not null)
        {
            return WithTimer(DefaultStatusLines(Wrap(view.GlobalError, Math.Max(1, terminalWidth - 4))), view.TimerStatus, view.TimerIsBright, view.PomodoroCompletion);
        }

        if (view.State.Editor is not null)
        {
            return WithTimer(TodoTaskEditorStatus(
                view.State.Editor,
                view.Projects
                    .Where(project => project.Project is not null)
                    .Select(project => new TodoEditorProjectOption(project.Title, project.Project!.Path))
                    .ToArray(),
                keyBindings,
                terminalWidth,
                terminalHeight), view.TimerStatus, view.TimerIsBright, view.PomodoroCompletion);
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

            return WithTimer(DefaultStatusLines(menuLines
                .SelectMany(line => Wrap(line, Math.Max(1, terminalWidth - 4)))
                .ToArray()), view.TimerStatus, view.TimerIsBright, view.PomodoroCompletion);
        }

        var status = view.State switch
        {
            { IsFilterMode: true } => $"/{view.State.FilterDraft}",
            { Error: not null } => view.State.Error,
            { StatusMessage: not null } => view.State.StatusMessage,
            { FilterText.Length: > 0 } =>
                $"FILTER: /{view.State.FilterText}  {Shortest(keyBindings.FilterMode)} EDIT  " +
                $"EMPTY Enter CLEARS  {SortHint(view.State, keyBindings)}",
            _ when compact => CompactStatus(keyBindings, view.State),
            _ => NormalStatus(keyBindings, view.State)
        };

        return WithTimer(
            DefaultStatusLines(Wrap(status, Math.Max(1, terminalWidth - 4))),
            view.TimerStatus,
            view.TimerIsBright,
            view.PomodoroCompletion);
    }

    public IReadOnlyList<BrowserStatusLine> PlannerStatus(
        PlannerView view,
        TuiKeyBindings keyBindings,
        int terminalWidth,
        int terminalHeight)
    {
        IReadOnlyList<string> status;
        if (view.CommandPalette is not null)
        {
            return WithTimer(DefaultStatusLines([CommandPaletteFooter(keyBindings)]), view.TimerStatus, view.TimerIsBright, view.PomodoroCompletion);
        }

        if (view.State.Editor is not null)
        {
            return WithTimer(TodoTaskEditorStatus(
                view.State.Editor,
                view.Projects
                    .Select(project => new TodoEditorProjectOption(project.Title, project.Path))
                    .ToArray(),
                keyBindings,
                terminalWidth,
                terminalHeight), view.TimerStatus, view.TimerIsBright, view.PomodoroCompletion);
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
        else if (view.CalendarAgenda.Error is not null)
        {
            status = [$"{view.CalendarAgenda.Error}  {Shortest(keyBindings.PlannerRefreshCalendar)} RETRY"];
        }
        else
        {
            status = view.State.Mode switch
            {
                WolfTodo.Tui.Features.DayPlanner.PlannerMode.EditFilter or
                    WolfTodo.Tui.Features.DayPlanner.PlannerMode.ChooseTodo => [PlannerPickerFooter(keyBindings)],
                WolfTodo.Tui.Features.DayPlanner.PlannerMode.MoveTodo =>
                [
                    $"MOVE TODO  {Shortest(keyBindings.FocusNext)} PANE  " +
                    $"{Shortest(keyBindings.MoveDown)}/{Shortest(keyBindings.MoveUp)} ITEM  " +
                    $"{Shortest(keyBindings.JumpTop)}/{Shortest(keyBindings.JumpBottom)} TOP/BOTTOM  " +
                    $"{Shortest(keyBindings.PlannerPreviousDay)}/{Shortest(keyBindings.PlannerNextDay)} DAY  " +
                    $"{Shortest(keyBindings.Open)} PLACE  {Shortest(keyBindings.Back)} CANCEL"
                ],
                _ =>
                [
                    $"{Shortest(keyBindings.FocusNext)} PANE  " +
                    $"{Shortest(keyBindings.MoveDown)}/{Shortest(keyBindings.MoveUp)} ITEM  " +
                    $"{Shortest(keyBindings.JumpTop)}/{Shortest(keyBindings.JumpBottom)} TOP/BOTTOM  " +
                    $"{Shortest(keyBindings.PlannerPreviousDay)}/{Shortest(keyBindings.PlannerNextDay)} DAY  " +
                    $"{Shortest(keyBindings.PlannerToday)} TODAY  {Shortest(keyBindings.Open)} ASSIGN/MOVE  " +
                    $"{Shortest(keyBindings.FilterMode)} FILTER  " +
                    $"{Shortest(keyBindings.PlannerUnschedule)} UNSCHEDULE  " +
                    $"{Shortest(keyBindings.PlannerExportSchedule)} EXPORT  " +
                    $"{Shortest(keyBindings.CreateTodo)} CREATE  {Shortest(keyBindings.EditTodo)} EDIT  " +
                    $"{Shortest(keyBindings.ToggleTodo)} COMPLETE  {Shortest(keyBindings.ToggleDetails)} DETAILS" +
                    (view.CalendarAgenda.SyncState == PlannerCalendarSyncState.Disabled
                        ? string.Empty
                        : $"  {Shortest(keyBindings.PlannerRefreshCalendar)} CALENDAR {CalendarStatus(view.CalendarAgenda)}")
                ]
            };

            if (view.CalendarAgenda.Warning is not null)
            {
                status =
                [
                    .. status,
                    $"{view.CalendarAgenda.Warning}  {Shortest(keyBindings.PlannerRefreshCalendar)} RETRY"
                ];
            }
        }

        var statusWidth = Math.Max(1, terminalWidth - 4);
        return WithTimer(
            DefaultStatusLines(status.SelectMany(line => Wrap(line, statusWidth))),
            view.TimerStatus,
            view.TimerIsBright,
            view.PomodoroCompletion);
    }

    public string BrowserMode(BrowserView view) => view switch
    {
        { PomodoroPrompt: not null } => "POMODORO",
        { CommandPalette: not null } => "HELP",
        { GlobalCommand: not null } => "COMMAND",
        { GlobalError: not null } => "ERROR",
        { State.Editor.IsCreate: true } => "CREATE",
        { State.Editor: not null } => "EDIT",
        { State.BulkEditor: not null } => "BULK",
        { State.IsFilterMode: true } => "FILTER",
        { State.IsSortMode: true } => "SORT",
        { State.Error: not null } => "ERROR",
        _ => "BROWSE"
    };

    public string PlannerMode(PlannerView view) => view switch
    {
        { PomodoroPrompt: not null } => "POMODORO",
        { CommandPalette: not null } => "HELP",
        { GlobalCommand: not null } => "COMMAND",
        { GlobalError: not null } => "ERROR",
        { State.Editor.IsCreate: true } => "CREATE",
        { State.Editor: not null } => "EDIT",
        { State.Mode: WolfTodo.Tui.Features.DayPlanner.PlannerMode.EditFilter } => "FILTER",
        { State.Mode: WolfTodo.Tui.Features.DayPlanner.PlannerMode.ChooseTodo } => "PICK",
        { State.Mode: WolfTodo.Tui.Features.DayPlanner.PlannerMode.MoveTodo } => "MOVE",
        { State.Error: not null } => "ERROR",
        _ => "BROWSE"
    };

    public string SortHint(BrowserState state, TuiKeyBindings bindings)
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

    public IReadOnlyList<string> Wrap(string value, int width)
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

    public string CommandPaletteFooter(TuiKeyBindings bindings) =>
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
        $"{Shortest(bindings.FilterMode)} SEARCH  {Shortest(bindings.Open)} RUN  " +
        $"{Shortest(bindings.Back)} CLOSE";

    public string PlannerPickerFooter(TuiKeyBindings bindings) =>
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
        $"{Shortest(bindings.Open)} ASSIGN  {Shortest(bindings.FilterMode)} FILTER  " +
        $"{Shortest(bindings.Back)} CANCEL";

    public string CalendarStatus(PlannerCalendarAgenda agenda) => agenda.SyncState switch
    {
        PlannerCalendarSyncState.Syncing => "SYNCING",
        PlannerCalendarSyncState.Ready => "READY",
        PlannerCalendarSyncState.AuthenticationRequired => "SIGN IN",
        PlannerCalendarSyncState.ConfigurationError => "CONFIG",
        PlannerCalendarSyncState.Offline => "OFFLINE",
        _ => string.Empty
    };

    public string Shortest(System.Collections.Immutable.ImmutableArray<KeyGesture> gestures) =>
        TuiKeyBindings.ShortestDisplayName(gestures);

    public IReadOnlyList<BrowserStatusLine> TodoTaskEditorStatus(
        TodoTaskEditorState editor,
        IReadOnlyList<TodoEditorProjectOption> projects,
        TuiKeyBindings bindings,
        int terminalWidth,
        int terminalHeight) =>
        TodoTaskEditorDialog.Create(editor, bindings, terminalWidth, terminalHeight).Lines
            .Select(line => new BrowserStatusLine(
                line.Text,
                line.Role switch
                {
                    TodoTaskEditorDialogRole.Label => BrowserStatusRole.FormLabel,
                    TodoTaskEditorDialogRole.Value => BrowserStatusRole.FormValue,
                    TodoTaskEditorDialogRole.ActiveValue => BrowserStatusRole.FormActiveValue,
                    TodoTaskEditorDialogRole.Placeholder => BrowserStatusRole.FormPlaceholder,
                    TodoTaskEditorDialogRole.Hint => BrowserStatusRole.FormHint,
                    TodoTaskEditorDialogRole.Error => BrowserStatusRole.FormError,
                    TodoTaskEditorDialogRole.Warning => BrowserStatusRole.ContentWarning,
                    _ => BrowserStatusRole.Default
                }))
            .ToArray();

    public IReadOnlyList<BrowserStatusLine> DefaultStatusLines(IEnumerable<string> lines) =>
        lines.Select(line => new BrowserStatusLine(line)).ToArray();

    private static IReadOnlyList<BrowserStatusLine> WithTimer(
        IReadOnlyList<BrowserStatusLine> lines,
        string? timerStatus,
        bool timerIsBright,
        PomodoroCompletion? completion) => timerStatus is not null
        ? [new BrowserStatusLine(
            timerStatus,
            timerIsBright ? BrowserStatusRole.TimerActive : BrowserStatusRole.TimerInactive), .. lines]
        : completion is not null
            ? [new BrowserStatusLine(completion.Status, BrowserStatusRole.PomodoroComplete), .. lines]
            : lines;

    public string NormalStatus(TuiKeyBindings bindings, BrowserState state) =>
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} NAVIGATE  " +
        $"{Shortest(bindings.FocusNext)} PANE  {Shortest(bindings.Open)} OPEN  {Shortest(bindings.Back)} BACK  " +
        $"{Shortest(bindings.FilterMode)} FILTER  {Shortest(bindings.CommandMode)} COMMAND  " +
        $"{Shortest(bindings.ToggleDetails)} DETAILS  " +
        $"{MarkedHint(state, bindings)}" +
        $"{bindings.ToggleCompletedCommand}  {bindings.QuitCommand}  {SortHint(state, bindings)}";

    public string CompactStatus(TuiKeyBindings bindings, BrowserState state) =>
        $"{Shortest(bindings.MoveDown)}/{Shortest(bindings.MoveUp)} MOVE  " +
        $"{Shortest(bindings.Back)}/{Shortest(bindings.Open)} BACK/OPEN  " +
        $"{Shortest(bindings.FilterMode)} FILTER  {Shortest(bindings.ToggleDetails)} DETAILS  " +
        $"{Shortest(bindings.CommandMode)} COMMANDS  " +
        $"{MarkedHint(state, bindings)}" +
        SortHint(state, bindings);

    private string MarkedHint(BrowserState state, TuiKeyBindings bindings) =>
        state.MarkedTodos.Count == 0
            ? string.Empty
            : $"{state.MarkedTodos.Count} MARKED  {Shortest(bindings.ToggleTodoSelection)} MARK  " +
              $"{Shortest(bindings.BulkEditTodos)} BULK  " +
              $"{Shortest(bindings.ClearTodoSelection)} CLEAR  ";

    public void WriteBrowserStatus(
        IReadOnlyList<BrowserStatusLine> lines,
        BrowserView view,
        TuiTheme theme,
        TodoTaskEditorDialogView? editorDialog = null)
    {
        if (editorDialog is not null)
        {
            themeRenderer.WriteSurface(TodoTaskEditorDialog.CreateRenderable(editorDialog, theme), theme.Surface2, true);
            return;
        }

        var defaultStyle = view.State switch
        {
            _ when view.GlobalError is not null || view.CommandPalette?.State.Error is not null =>
                themeRenderer.Style(theme.Error, Decoration.Bold),
            _ when view.GlobalCommand is not null || view.CommandPalette is not null => themeRenderer.Style(theme.Accent),
            { Error: not null } => themeRenderer.Style(theme.Error, Decoration.Bold),
            { StatusMessage: not null } => themeRenderer.Style(theme.Success, Decoration.Bold),
            { IsFilterMode: true } => themeRenderer.Style(theme.Accent),
            { IsSortMode: true } => themeRenderer.Style(theme.Accent),
            { Editor: not null } => themeRenderer.Style(theme.Accent),
            _ => themeRenderer.Style(theme.SecondaryText)
        };
        var statusIsActive = view.GlobalCommand is not null ||
                             view.CommandPalette is not null ||
                             view.State.IsFilterMode ||
                             view.State.IsSortMode ||
                             view.State.Editor is not null;
        WriteStatusPanel(lines, theme, defaultStyle, statusIsActive);
    }

    public void WritePlannerStatus(
        IReadOnlyList<BrowserStatusLine> lines,
        PlannerView view,
        TuiTheme theme,
        TodoTaskEditorDialogView? editorDialog = null)
    {
        if (editorDialog is not null)
        {
            themeRenderer.WriteSurface(TodoTaskEditorDialog.CreateRenderable(editorDialog, theme), theme.Surface2, true);
            return;
        }

        var defaultStyle = view.GlobalError is not null ||
                           view.State.Error is not null ||
                           view.CommandPalette?.State.Error is not null
            ? themeRenderer.Style(theme.Error, Decoration.Bold)
            : view.GlobalCommand is not null || view.CommandPalette is not null
                ? themeRenderer.Style(theme.Accent)
                : view.State.Mode == WolfTodo.Tui.Features.DayPlanner.PlannerMode.Browse
                    ? themeRenderer.Style(theme.SecondaryText)
                    : themeRenderer.Style(theme.Accent);
        var statusIsActive = view.GlobalCommand is not null ||
                             view.CommandPalette is not null ||
                             view.State.Mode != WolfTodo.Tui.Features.DayPlanner.PlannerMode.Browse;
        WriteStatusPanel(lines, theme, defaultStyle, statusIsActive);
    }

    public void WriteStatusPanel(
        IReadOnlyList<BrowserStatusLine> lines,
        TuiTheme theme,
        Style defaultStyle,
        bool statusIsActive)
    {
        var content = lines.Select(line => new Text(
            line.Text,
            line.Role switch
            {
                BrowserStatusRole.FormLabel => themeRenderer.Style(theme.Heading, Decoration.Bold),
                BrowserStatusRole.FormValue => themeRenderer.Style(theme.SecondaryText),
                BrowserStatusRole.FormActiveValue => themeRenderer.Style(theme.AccentBright, Decoration.Bold),
                BrowserStatusRole.FormPlaceholder => themeRenderer.Style(theme.Muted, Decoration.Dim),
                BrowserStatusRole.FormHint => themeRenderer.Style(theme.Muted, Decoration.Dim),
                BrowserStatusRole.FormError => themeRenderer.Style(theme.Error, Decoration.Bold),
                BrowserStatusRole.ContentWarning => themeRenderer.Style(theme.Warning, Decoration.Bold),
                BrowserStatusRole.TimerActive => themeRenderer.Style(theme.Timer, Decoration.Bold),
                BrowserStatusRole.TimerInactive => themeRenderer.Style(theme.Timer, Decoration.Dim),
                BrowserStatusRole.PomodoroComplete => themeRenderer.Style(theme.Timer, Decoration.Bold),
                _ => defaultStyle
            }));
        themeRenderer.WriteSurface(
            new Panel(new Rows(content))
            {
                Border = BoxBorder.Square,
                BorderStyle = themeRenderer.Style(statusIsActive ? theme.BorderActive : theme.Border),
                Expand = true
            },
            theme.Surface2,
            true);
    }
}
