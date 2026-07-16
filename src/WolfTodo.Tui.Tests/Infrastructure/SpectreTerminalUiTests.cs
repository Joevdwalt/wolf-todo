using FluentAssertions;
using System.Collections.Immutable;
using Spectre.Console;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Infrastructure;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class SpectreTerminalUiTests
{
    private static readonly TuiKeyBindings DefaultBindings = TuiKeyBindings.CreateDefaults(":q");
    private static readonly TabStripView DefaultTabs = new(
        [new TabItemView(new TabId("todos"), "Todos", true)]);

    [Fact]
    public void ShowSplash_applies_the_configured_semantic_colors()
    {
        var theme = TuiThemes.Wolf with
        {
            Accent = new Color(1, 2, 3),
            Heading = new Color(4, 5, 6),
            Muted = new Color(7, 8, 9)
        };
        StartRecording();

        new SpectreTerminalUi(() => 140, () => 30).ShowSplash("WOLF", theme);
        var html = AnsiConsole.ExportHtml().ToLowerInvariant();

        html.Should().Contain("#010203")
            .And.Contain("#040506")
            .And.Contain("#070809");
    }

    [Fact]
    public void ShowBrowser_renders_and_updates_the_selected_project_and_todo()
    {
        var todo = new TodoItem(
            1,
            false,
            "134416",
            "Milas Contract Renewal",
            TodoPriority.High,
            ["now"],
            new DateOnly(2026, 7, 8),
            null,
            "Renewals",
            [new TodoNote(2, "Review current contract")],
            []);
        var state = BrowserState.Initial;
        var view = new BrowserView(
            state,
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, 0, true)],
            todo,
            "All",
            "/todos/contracts.md",
            null,
            string.Empty);
        var terminal = new SpectreTerminalUi(() => 140, () => 30);
        StartRecording();

        terminal.ShowBrowser(DefaultTabs, view, DefaultBindings);
        terminal.ShowBrowser(DefaultTabs, view with { SelectedProjectTitle = "Personal" }, DefaultBindings);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("All").And.Contain("Personal").And.Contain("Milas Contract Renewal");
        output.Should().Contain("Projects").And.Contain("Todos: All").And.Contain("Details");
    }

    [Fact]
    public void ShowBrowser_applies_the_configured_semantic_colors()
    {
        var view = ViewWithTitle("Renew contract");
        var theme = TuiThemes.Wolf with
        {
            Accent = new Color(1, 2, 3),
            Heading = new Color(4, 5, 6),
            Border = new Color(7, 8, 9),
            Muted = new Color(10, 11, 12)
        };
        StartRecording();

        new SpectreTerminalUi(() => 140, () => 30)
            .ShowBrowser(DefaultTabs, view, DefaultBindings, theme);
        var html = AnsiConsole.ExportHtml().ToLowerInvariant();

        html.Should().Contain("#010203")
            .And.Contain("#040506")
            .And.Contain("#070809")
            .And.Contain("#0a0b0c");
    }

    [Fact]
    public void ShowBrowser_renders_global_command_input_and_errors()
    {
        var baseView = ViewWithTitle("Renew contract");
        StartRecording();
        var terminal = new SpectreTerminalUi(() => 140, () => 30);

        terminal.ShowBrowser(
            DefaultTabs,
            baseView with { GlobalCommand = ":q" },
            DefaultBindings,
            TuiThemes.Wolf);
        terminal.ShowBrowser(
            DefaultTabs,
            baseView with { GlobalError = "Unknown command: :wat" },
            DefaultBindings,
            TuiThemes.Wolf);
        var output = AnsiConsole.ExportText();

        output.Should().Contain(":q").And.Contain("Unknown command: :wat");
    }

    [Fact]
    public void ShowPlanner_renders_the_day_grid_and_configured_hints()
    {
        var date = new DateOnly(2026, 7, 15);
        var todo = new TodoItem(
            1, false, null, "Prepare proposal", null, [], null, null, string.Empty, [], [])
        {
            Schedule = new TodoSchedule(date, new TimeOnly(6, 0))
        };
        var catalog = new ProjectCatalog(
            [new TodoProject("Work", "/todos/work.md", [todo])],
            []);
        var view = new DayPlannerPresenter().CreateView(catalog, PlannerState.CreateInitial(date));
        var tabs = new TabStripView(
        [
            new TabItemView(new TabId("todos"), "Todos", false),
            new TabItemView(new TabId("planner"), "Day Planner", true)
        ]);
        StartRecording(100, 24);

        new SpectreTerminalUi(() => 100, () => 24)
            .ShowPlanner(tabs, view, DefaultBindings, TuiThemes.Wolf);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("[ Day Planner ]")
            .And.Contain("06:00")
            .And.Contain("Prepare proposal")
            .And.Contain("[/] day")
            .And.Contain("g today");
    }

    [Fact]
    public void ShowPlanner_renders_global_command_input_and_errors()
    {
        var date = new DateOnly(2026, 7, 15);
        var catalog = new ProjectCatalog([], []);
        var baseView = new DayPlannerPresenter().CreateView(catalog, PlannerState.CreateInitial(date));
        var commandView = baseView with { GlobalCommand = ":q" };
        var errorView = baseView with { GlobalError = "Unknown command: :wat" };
        StartRecording(100, 24);

        var terminal = new SpectreTerminalUi(() => 100, () => 24);
        terminal.ShowPlanner(DefaultTabs, commandView, DefaultBindings, TuiThemes.Wolf);
        terminal.ShowPlanner(DefaultTabs, errorView, DefaultBindings, TuiThemes.Wolf);
        var output = AnsiConsole.ExportText();

        output.Should().Contain(":q").And.Contain("Unknown command: :wat");
    }

    [Fact]
    public void ShowBrowser_renders_the_content_editor_and_command_palette()
    {
        var view = ViewWithTitle("Parent");
        var identity = view.SelectedTodoIdentity!;
        var editor = TodoContentEditorState.Create(identity, view.SelectedTodo!);
        var paletteState = CommandPaletteState.Closed with { IsOpen = true };
        var palette = new CommandPaletteView(
            paletteState,
            [new CommandPaletteItem(
                ApplicationActionId.BrowserEditContent,
                "Todos",
                "Edit notes and subtasks",
                "Open content editor",
                "E",
                true,
                null)]);
        StartRecording(100, 24);
        var terminal = new SpectreTerminalUi(() => 100, () => 24);

        terminal.ShowBrowser(
            DefaultTabs,
            view with { State = view.State with { ContentEditor = editor } },
            DefaultBindings,
            TuiThemes.Wolf);
        terminal.ShowBrowser(
            DefaultTabs,
            view with { CommandPalette = palette },
            DefaultBindings,
            TuiThemes.Wolf);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("Content: Parent")
            .And.Contain("Notes")
            .And.Contain("Subtasks")
            .And.Contain("Command palette")
            .And.Contain("Edit notes and subtasks");
    }

    [Theory]
    [InlineData(70, 16)]
    [InlineData(80, 18)]
    [InlineData(100, 24)]
    public void ShowPlanner_leaves_the_final_terminal_row_free_and_keeps_tabs_visible(
        int width,
        int height)
    {
        var lines = RenderPlanner(width, height);

        lines.Should().HaveCount(height - 1);
        lines[0].Should().Contain("[ Day Planner ]");
    }

    [Fact]
    public void ShowPlanner_keeps_tabs_visible_while_the_command_palette_is_open()
    {
        var paletteState = CommandPaletteState.Closed with { IsOpen = true };
        var items = Enumerable.Range(1, 12)
            .Select(index => new CommandPaletteItem(
                ApplicationActionId.PlannerToday,
                "Planner",
                $"Action {index}",
                "Planner action",
                "g",
                true,
                null))
            .ToImmutableArray();

        var lines = RenderPlanner(70, 16, new CommandPaletteView(paletteState, items));

        lines.Should().HaveCount(15);
        lines[0].Should().Contain("[ Day Planner ]");
    }

    [Fact]
    public void ShowBrowser_keeps_wide_column_boundaries_when_details_wrap()
    {
        var shortView = ViewWithTitle("Short title");
        var longView = ViewWithTitle(new string('x', 160));

        var shortHeader = RenderHeader(shortView);
        var longHeader = RenderHeader(longView);

        longHeader.IndexOf("Todos:", StringComparison.Ordinal)
            .Should().Be(shortHeader.IndexOf("Todos:", StringComparison.Ordinal));
        longHeader.IndexOf("Details", StringComparison.Ordinal)
            .Should().Be(shortHeader.IndexOf("Details", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_truncates_todo_titles_and_keeps_metadata_in_details()
    {
        const string title = "Prepare the unusually detailed contract renewal proposal for the customer before the quarterly review meeting";
        var todo = new TodoItem(
            1,
            false,
            "134416",
            title,
            TodoPriority.High,
            ["now"],
            new DateOnly(2026, 7, 8),
            new DateOnly(2026, 7, 12),
            "Renewals",
            [new TodoNote(2, "Review current contract")],
            []);
        var view = new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, 0, true)],
            todo,
            "All",
            "/todos/contracts.md",
            null,
            string.Empty);

        StartRecording();
        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view, DefaultBindings);
        var output = AnsiConsole.ExportText();
        var todoLine = output.Split(Environment.NewLine)
            .Last(line => line.Contains("Prepare the unusually", StringComparison.Ordinal));
        var todoPane = todoLine.Split('│')[2];

        todoPane.Should().Contain("[ ] ⏫ Prepare the unusually").And.Contain("…");
        todoPane.Should().NotContain("134416").And.NotContain("#now").And.NotContain("2026-07-08");
        output.Should().Contain("quarterly review")
            .And.Contain("meeting")
            .And.Contain("Reference: 134416")
            .And.Contain("Tags: #now")
            .And.Contain("Start: 2026-07-08")
            .And.Contain("Due: 2026-07-12");
    }

    [Fact]
    public void ShowBrowser_renders_priority_between_status_and_title_for_todos_and_subtasks()
    {
        var priorities = new[]
        {
            (TodoPriority.Highest, "🔺"),
            (TodoPriority.High, "⏫"),
            (TodoPriority.Medium, "🔼"),
            (TodoPriority.Low, "🔽"),
            (TodoPriority.Lowest, "⏬")
        };
        var todos = priorities.Select((priority, index) => new TodoItem(
            index + 1,
            false,
            null,
            $"Priority {priority.Item1}",
            priority.Item1,
            [],
            null,
            null,
            string.Empty,
            [],
            [])).ToArray();
        var subtask = new TodoItem(
            20,
            true,
            null,
            "Nested task",
            TodoPriority.Medium,
            [],
            null,
            null,
            string.Empty,
            [],
            []);
        var selectedTodo = todos[0] with { Subtasks = [subtask] };
        var view = new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("All", todos.Length, null, null, true)],
            [.. todos.Select((todo, index) => new TodoRow(null, todo, 0, index == 0))],
            selectedTodo,
            "All",
            "/todos/project.md",
            null,
            string.Empty);

        StartRecording();
        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view, DefaultBindings);
        var output = AnsiConsole.ExportText();

        foreach (var (priority, marker) in priorities)
        {
            output.Should().Contain($"[ ] {marker} Priority {priority}");
        }

        output.Should().Contain("[x] 🔼 Nested task");
    }

    [Theory]
    [InlineData(140, 30)]
    [InlineData(100, 20)]
    [InlineData(70, 16)]
    public void ShowBrowser_renders_schedules_beneath_and_aligned_with_todo_titles(
        int width,
        int height)
    {
        var scheduled = new TodoItem(
            1, false, null, "Prepare proposal", TodoPriority.High, [], null, null, string.Empty, [], [])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 30))
        };
        var nested = new TodoItem(
            2, true, null, "Nested follow-up", null, [], null, null, string.Empty, [], [])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 7, 16), new TimeOnly(10, 0))
        };
        var view = new BrowserView(
            BrowserState.Initial with { Focus = BrowserFocus.Todos },
            [new ProjectRow("All", 2, null, null, true)],
            [new TodoRow(null, scheduled, 0, true), new TodoRow(null, nested, 1, false)],
            scheduled,
            "All",
            "/todos/project.md",
            null,
            string.Empty);

        var lines = RenderBrowser(view, width, height);
        var scheduledTitle = Array.FindIndex(lines, line => line.Contains("Prepare proposal", StringComparison.Ordinal));
        var nestedTitle = Array.FindIndex(lines, line => line.Contains("Nested follow-up", StringComparison.Ordinal));

        scheduledTitle.Should().BeGreaterThanOrEqualTo(0);
        lines[scheduledTitle + 1].Should().Contain("⏳ 2026-07-15 09:30");
        lines[scheduledTitle][..lines[scheduledTitle].IndexOf("Prepare proposal", StringComparison.Ordinal)]
            .GetCellWidth()
            .Should().Be(lines[scheduledTitle + 1]
                [..lines[scheduledTitle + 1].IndexOf("⏳", StringComparison.Ordinal)]
                .GetCellWidth());
        nestedTitle.Should().BeGreaterThanOrEqualTo(0);
        lines[nestedTitle].Should().Contain("[x] Nested follow-up");
        lines[nestedTitle + 1].Should().Contain("⏳ 2026-07-16 10:00");
        lines[nestedTitle][..lines[nestedTitle].IndexOf("Nested follow-up", StringComparison.Ordinal)]
            .GetCellWidth()
            .Should().Be(lines[nestedTitle + 1]
                [..lines[nestedTitle + 1].IndexOf("⏳", StringComparison.Ordinal)]
                .GetCellWidth());
    }

    [Fact]
    public void ShowBrowser_does_not_add_a_schedule_line_to_an_unscheduled_todo()
    {
        var lines = RenderBrowser(ViewWithTitle("Unscheduled task"), 140, 30);

        lines.Should().NotContain(line => line.Contains("⏳", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_renders_filter_editing_and_committed_filter_statuses()
    {
        var terminal = new SpectreTerminalUi(() => 140, () => 30);
        var view = ViewWithTitle("Renew contract");
        StartRecording();

        terminal.ShowBrowser(DefaultTabs, view with
        {
            State = view.State with { IsFilterMode = true, FilterDraft = "renew" }
        }, DefaultBindings);
        terminal.ShowBrowser(DefaultTabs, view with
        {
            State = view.State with { FilterText = "renew" }
        }, DefaultBindings);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("/renew");
        output.Should().Contain("Filter: /renew").And.Contain("empty Enter clears");
    }

    [Fact]
    public void ShowBrowser_renders_the_sort_dialog_and_active_sort_hint()
    {
        var view = ViewWithTitle("Renew contract");
        StartRecording();

        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view with
        {
            State = view.State with { IsSortMode = true }
        }, DefaultBindings);
        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view with
        {
            State = view.State with
            {
                Sort = new TodoSort(TodoSortProperty.Name, TodoSortDirection.Descending)
            }
        }, DefaultBindings);
        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view with
        {
            State = view.State with
            {
                Sort = new TodoSort(TodoSortProperty.Priority, TodoSortDirection.Ascending)
            }
        }, DefaultBindings);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("Sort: n/N name").And.Contain("p/P priority")
            .And.Contain("t/T tags").And.Contain("o source");
        output.Should().Contain("t name↓").And.Contain("t priority↑");
    }

    [Fact]
    public void ShowBrowser_fits_the_multiline_sort_dialog_without_scrolling_the_tabs()
    {
        var view = ViewWithTodoCount(1, BrowserFocus.Todos, scheduled: true);
        var lines = RenderBrowser(
            view with { State = view.State with { IsSortMode = true } },
            40,
            16);

        lines[0].Should().Contain("[ Todos ]");
        lines.Should().HaveCount(15);
        lines.Should().Contain(line => line.Contains("n/N name", StringComparison.Ordinal));
        lines.Should().Contain(line => line.Contains("p/P priority", StringComparison.Ordinal));
        lines.Should().Contain(line => line.Contains("Esc cancel", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_includes_the_filter_key_in_wide_and_compact_hints()
    {
        var view = ViewWithTitle("Renew contract");
        StartRecording();

        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view, DefaultBindings);
        new SpectreTerminalUi(() => 70, () => 16).ShowBrowser(DefaultTabs, view, DefaultBindings);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("/ filter  : command");
        output.Should().Contain("j/k move").And.Contain("h/l back/open");
    }

    [Fact]
    public void ShowBrowser_uses_the_shortest_configured_bindings_in_status_hints()
    {
        var view = ViewWithTitle("Renew contract");
        var bindings = TuiKeyBindings.CreateDefaults(":quit") with
        {
            MoveDown = [KeyGesture.Parse("Ctrl+N"), KeyGesture.Parse("n")],
            MoveUp = [KeyGesture.Parse("Ctrl+P"), KeyGesture.Parse("p")],
            FilterMode = [KeyGesture.Parse("Ctrl+F")],
            ToggleCompletedCommand = ":done"
        };
        StartRecording();
        var existingOutputLength = AnsiConsole.ExportText().Length;

        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view, bindings);
        var output = AnsiConsole.ExportText()[existingOutputLength..];

        output.Should().Contain("n/p navigate")
            .And.Contain("Ctrl+F filter")
            .And.Contain(":done")
            .And.Contain(":quit");
        output.Should().NotContain("Ctrl+N");
    }

    [Fact]
    public void ShowBrowser_always_renders_the_selected_tab_strip()
    {
        var output = RenderBrowser(ViewWithTitle("Renew contract"), 140, 30);

        output[0].Should().Contain("[ Todos ]");
        output[0].Should().NotContain("tabs");
    }

    [Fact]
    public void ShowBrowser_renders_multiple_tabs_and_the_switch_hint()
    {
        var tabs = new TabStripView(
        [
            new TabItemView(new TabId("todos"), "Todos", false),
            new TabItemView(new TabId("planner"), "Day Planner", true)
        ]);

        var output = RenderBrowser(tabs, ViewWithTitle("Renew contract"), 140, 30);

        output[0].Should().Contain("Todos").And.Contain("[ Day Planner ]");
        output[0].Should().Contain("L tabs");
    }

    [Theory]
    [InlineData(140, 30)]
    [InlineData(100, 20)]
    [InlineData(70, 16)]
    public void ShowBrowser_hides_details_and_gives_the_todo_view_the_available_space(
        int width,
        int height)
    {
        var view = ViewWithTitle("Renew contract");
        var output = RenderBrowser(
            view with
            {
                State = view.State with
                {
                    ShowDetails = false,
                    Focus = BrowserFocus.Details
                }
            },
            width,
            height);

        output.Should().NotContain(line => line.Contains("Details", StringComparison.Ordinal));
        output.Should().Contain(line => line.Contains("Todos: All", StringComparison.Ordinal));
        output.Should().HaveCountGreaterThanOrEqualTo(height - 1);
    }

    [Fact]
    public void ShowBrowser_truncates_the_tab_strip_on_a_narrow_terminal()
    {
        var tabs = new TabStripView(
        [
            new TabItemView(new TabId("todos"), "Todos With An Extremely Long Name", true),
            new TabItemView(new TabId("planner"), "Day Planner", false)
        ]);

        var output = RenderBrowser(tabs, ViewWithTitle("Renew contract"), 24, 16);

        output[0].Should().Contain("…");
    }

    [Theory]
    [InlineData(140, 30, BrowserFocus.Projects)]
    [InlineData(100, 20, BrowserFocus.Todos)]
    [InlineData(70, 16, BrowserFocus.Todos)]
    public void ShowBrowser_keeps_the_status_position_when_filtering_reduces_results(
        int width,
        int height,
        BrowserFocus focus)
    {
        var unfiltered = RenderBrowser(ViewWithTodoCount(8, focus, scheduled: true), width, height);
        var filteredView = ViewWithTodoCount(1, focus, scheduled: true);
        var filtered = RenderBrowser(
            filteredView with { State = filteredView.State with { FilterText = "Todo 1" } },
            width,
            height);

        unfiltered.Length.Should().BeGreaterThanOrEqualTo(height - 1);
        filtered.Length.Should().BeGreaterThanOrEqualTo(height - 1);
        StatusPanelTop(unfiltered).Should().Be(StatusPanelTop(filtered));
    }

    [Fact]
    public void ShowBrowser_limits_long_lists_to_the_available_terminal_height()
    {
        var lines = RenderBrowser(ViewWithTodoCount(25, BrowserFocus.Todos), 140, 24);

        lines.Should().HaveCount(23);
        lines.Should().NotContain(line => line.Contains("Todo 25", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_keeps_the_selected_todo_in_the_visible_window()
    {
        var lines = RenderBrowser(ViewWithTodoCount(25, BrowserFocus.Todos, 24), 140, 24);

        lines.Should().HaveCount(23);
        lines.Should().Contain(line => line.Contains("Todo 25", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_keeps_a_selected_todo_and_its_schedule_together_in_the_visible_window()
    {
        var lines = RenderBrowser(
            ViewWithTodoCount(25, BrowserFocus.Todos, selectedIndex: 24, scheduled: true),
            140,
            24);
        var selectedLine = Array.FindIndex(lines, line => line.Contains("Todo 25", StringComparison.Ordinal));
        var firstTodoContent = Array.FindIndex(lines, line =>
            line.Contains("Todo ", StringComparison.Ordinal) || line.Contains("⏳", StringComparison.Ordinal));

        lines.Should().HaveCount(23);
        selectedLine.Should().BeGreaterThanOrEqualTo(0);
        lines[selectedLine + 1].Should().Contain("⏳ 2026-07-15 09:30");
        lines[firstTodoContent].Should().Contain("Todo ");
    }

    [Fact]
    public void ShowBrowser_leaves_the_final_terminal_row_free_to_avoid_scrolling_the_tabs()
    {
        var lines = RenderBrowser(ViewWithTodoCount(1, BrowserFocus.Todos), 140, 30);

        lines.Should().HaveCount(29);
    }

    private static BrowserView ViewWithTitle(string title)
    {
        var todo = new TodoItem(1, false, null, title, null, [], null, null, string.Empty, [], []);
        return new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, 0, true)],
            todo,
            "All",
            "/todos/project.md",
            null,
            string.Empty);
    }

    private static BrowserView ViewWithTodoCount(
        int count,
        BrowserFocus focus,
        int selectedIndex = 0,
        bool scheduled = false)
    {
        var todos = Enumerable.Range(1, count)
            .Select(index => new TodoItem(
                index,
                false,
                null,
                $"Todo {index}",
                null,
                [],
                null,
                null,
                string.Empty,
                [],
                [])
            {
                Schedule = scheduled
                    ? new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 30))
                    : null
            })
            .ToArray();
        var rows = todos.Select((todo, index) => new TodoRow(null, todo, 0, index == selectedIndex)).ToArray();

        return new BrowserView(
            BrowserState.Initial with { Focus = focus },
            [new ProjectRow("All", count, null, null, true)],
            [.. rows],
            todos[selectedIndex],
            "All",
            "/todos/project.md",
            null,
            string.Empty);
    }

    private static string[] RenderBrowser(BrowserView view, int width, int height)
    {
        return RenderBrowser(DefaultTabs, view, width, height);
    }

    private static string[] RenderBrowser(
        TabStripView tabs,
        BrowserView view,
        int width,
        int height)
    {
        StartRecording(width, height);
        var existingOutputLength = AnsiConsole.ExportText().Length;
        new SpectreTerminalUi(() => width, () => height).ShowBrowser(tabs, view, DefaultBindings);
        return AnsiConsole.ExportText()[existingOutputLength..]
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string[] RenderPlanner(
        int width,
        int height,
        CommandPaletteView? palette = null)
    {
        var date = new DateOnly(2026, 7, 15);
        var tabs = new TabStripView(
        [
            new TabItemView(new TabId("todos"), "Todos", false),
            new TabItemView(new TabId("planner"), "Day Planner", true)
        ]);
        var view = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([], []),
            PlannerState.CreateInitial(date)) with { CommandPalette = palette };
        StartRecording(width, height);
        var existingOutputLength = AnsiConsole.ExportText().Length;
        new SpectreTerminalUi(() => width, () => height)
            .ShowPlanner(tabs, view, DefaultBindings, TuiThemes.Wolf);
        return AnsiConsole.ExportText()[existingOutputLength..]
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    private static int StatusPanelTop(string[] lines) =>
        Array.FindLastIndex(lines, line => line.StartsWith('╭'));

    private static string RenderHeader(BrowserView view)
    {
        StartRecording();
        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view, DefaultBindings);
        return AnsiConsole.ExportText()
            .Split(Environment.NewLine)
            .First(line => line.Contains("Projects", StringComparison.Ordinal));
    }

    private static void StartRecording()
    {
        StartRecording(140, 30);
    }

    private static void StartRecording(int width, int height)
    {
        AnsiConsole.Record();
        AnsiConsole.Profile.Width = width;
        AnsiConsole.Profile.Height = height;
    }
}
