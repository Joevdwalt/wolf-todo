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
            Muted = new Color(7, 8, 9),
            Background = new Color(10, 11, 12)
        };
        StartRecording();

        new SpectreTerminalUi(() => 140, () => 30).ShowSplash("WOLF", theme);
        var html = AnsiConsole.ExportHtml().ToLowerInvariant();

        html.Should().Contain("#010203")
            .And.Contain("#040506")
            .And.Contain("#070809")
            .And.Contain("#0a0b0c");
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
            [new TodoRow(null, todo, [], true)],
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
        output.Should().Contain("PROJECTS").And.Contain("TODOS: ALL").And.Contain("DETAILS");
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
            BorderActive = new Color(7, 8, 9),
            Muted = new Color(10, 11, 12),
            Background = new Color(13, 14, 15),
            Surface = new Color(16, 17, 18),
            Surface2 = new Color(19, 20, 21)
        };
        StartRecording();

        new SpectreTerminalUi(() => 140, () => 30)
            .ShowBrowser(DefaultTabs, view, DefaultBindings, theme);
        var html = AnsiConsole.ExportHtml().ToLowerInvariant();

        html.Should().Contain("#010203")
            .And.Contain("#040506")
            .And.Contain("#070809")
            .And.Contain("#0a0b0c")
            .And.Contain("#0d0e0f")
            .And.Contain("#101112")
            .And.Contain("#131415");
    }

    [Fact]
    public void ShowBrowser_renders_today_as_a_date_colored_virtual_project()
    {
        var view = ViewWithTitle("Renew contract");
        view = view with
        {
            Projects =
            [
                new ProjectRow("All", 1, null, null, true, ProjectRowKind.All),
                new ProjectRow("@today", 1, null, null, false, ProjectRowKind.Today)
            ]
        };
        var theme = TuiThemes.Wolf with { Date = new Color(1, 2, 3) };
        StartRecording(140, 30);

        new SpectreTerminalUi(() => 140, () => 30)
            .ShowBrowser(DefaultTabs, view, DefaultBindings, theme);
        var output = AnsiConsole.ExportText();
        var html = NormalizeHtml(AnsiConsole.ExportHtml());

        output.Should().Contain("@today 1");
        StyleBefore(html, "@today").Should().Contain("#010203");
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

        output.Should().Contain("[DAY PLANNER]")
            .And.Contain("06:00")
            .And.Contain("Prepare proposal")
            .And.Contain("[/] DAY")
            .And.Contain("g TODAY");
    }

    [Fact]
    public void ShowPlanner_renders_all_day_items_and_meeting_overlap_warnings()
    {
        var date = new DateOnly(2026, 7, 15);
        var todo = CreateTodoItem("Prepare proposal", 1) with
        {
            Schedule = new TodoSchedule(date, new TimeOnly(9, 30))
        };
        var agenda = new PlannerCalendarAgenda(
            [new PlannerCalendarAllDayItem("Company holiday", PlannerCalendarItemKind.Event)],
            [new PlannerCalendarMeeting("Client meeting", new TimeOnly(9, 0), new TimeOnly(10, 0))],
            PlannerCalendarSyncState.Ready);
        var view = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([new TodoProject("Work", "/todos/work.md", [todo])], []),
            PlannerState.CreateInitial(date),
            agenda);
        StartRecording(140, 24);

        new SpectreTerminalUi(() => 140, () => 24)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, TuiThemes.Wolf);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("ALL DAY")
            .And.Contain("Company holiday")
            .And.Contain("Prepare proposal")
            .And.Contain("Client meeting");
    }

    [Fact]
    public void ShowPlanner_applies_canvas_workspace_overlay_and_active_roles()
    {
        var date = new DateOnly(2026, 7, 15);
        var view = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([], []),
            PlannerState.CreateInitial(date));
        var theme = TuiThemes.Wolf with
        {
            Background = new Color(1, 2, 3),
            Surface = new Color(4, 5, 6),
            Surface2 = new Color(7, 8, 9),
            BorderActive = new Color(10, 11, 12),
            AccentBright = new Color(13, 14, 15)
        };
        StartRecording(100, 24);

        new SpectreTerminalUi(() => 100, () => 24)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, theme);
        var html = AnsiConsole.ExportHtml().ToLowerInvariant();

        html.Should().Contain("#010203")
            .And.Contain("#040506")
            .And.Contain("#070809")
            .And.Contain("#0a0b0c")
            .And.Contain("#0d0e0f");
    }

    [Fact]
    public void ShowPlanner_renders_current_time_as_a_full_width_highlighted_timeline_row()
    {
        var date = new DateOnly(2026, 7, 15);
        var now = new DateTime(2026, 7, 15, 14, 23, 0);
        var state = PlannerState.CreateInitial(date) with { SlotIndex = 17 };
        var view = new DayPlannerPresenter().CreateView(new ProjectCatalog([], []), state);
        var theme = TuiThemes.Wolf with
        {
            AccentBright = new Color(1, 2, 3),
            Surface2 = new Color(4, 5, 6),
            BorderActive = new Color(7, 8, 9)
        };
        StartRecording(100, 24);
        var start = AnsiConsole.ExportText().Length;

        new SpectreTerminalUi(() => 100, () => 24, () => date, () => now)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, theme);
        var output = AnsiConsole.ExportText()[start..];
        var markerLine = output.Split(Environment.NewLine)
            .Single(line => line.Contains("14:23", StringComparison.Ordinal));
        var html = NormalizeHtml(AnsiConsole.ExportHtml());

        var cells = markerLine.Split('│');
        var planCell = cells[^2].Trim();

        planCell.Should().StartWith("▶");
        planCell[1..].Should().NotBeEmpty().And.MatchRegex("^─+$");
        planCell.Length.Should().Be(cells[^2].Length - 2);
        StyleBefore(html, "14:23").Should().Contain("#010203").And.NotContain("#040506");
        html.Should().Contain("#070809");
    }

    [Theory]
    [InlineData(5, 15, 0, "06:00", true)]
    [InlineData(14, 30, 17, "14:30", true)]
    [InlineData(22, 0, 31, "21:30", false)]
    public void ShowPlanner_places_the_current_time_at_its_timeline_boundary(
        int hour,
        int minute,
        int selectedSlot,
        string adjacentSlot,
        bool markerBeforeSlot)
    {
        var date = new DateOnly(2026, 7, 15);
        var now = new DateTime(2026, 7, 15, hour, minute, 0);
        var state = PlannerState.CreateInitial(date) with { SlotIndex = selectedSlot };
        var view = new DayPlannerPresenter().CreateView(new ProjectCatalog([], []), state);
        StartRecording(100, 24);
        var start = AnsiConsole.ExportText().Length;

        new SpectreTerminalUi(() => 100, () => 24, () => date, () => now)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, TuiThemes.Wolf);
        var lines = AnsiConsole.ExportText()[start..]
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var markerIndex = Array.FindIndex(lines, line => line.Contains($"{hour:00}:{minute:00}"));
        var slotIndex = Array.FindIndex(lines, line =>
            line.Contains(adjacentSlot) && line.Contains('—'));

        markerIndex.Should().BeGreaterThanOrEqualTo(0);
        slotIndex.Should().BeGreaterThanOrEqualTo(0);
        (markerIndex < slotIndex).Should().Be(markerBeforeSlot);
    }

    [Fact]
    public void ShowPlanner_prioritizes_a_distant_selected_slot_over_the_current_time_marker()
    {
        var date = new DateOnly(2026, 7, 15);
        var now = new DateTime(2026, 7, 15, 18, 17, 0);
        var view = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([], []),
            PlannerState.CreateInitial(date));
        StartRecording(70, 16);
        var start = AnsiConsole.ExportText().Length;

        new SpectreTerminalUi(() => 70, () => 16, () => date, () => now)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, TuiThemes.Wolf);
        var output = AnsiConsole.ExportText()[start..];

        output.Should().Contain("06:00").And.NotContain("18:17");
    }

    [Fact]
    public void ShowPlanner_omits_the_current_time_marker_for_another_date()
    {
        var selectedDate = new DateOnly(2026, 7, 16);
        var now = new DateTime(2026, 7, 15, 6, 17, 0);
        var view = new DayPlannerPresenter().CreateView(
            new ProjectCatalog([], []),
            PlannerState.CreateInitial(selectedDate));
        StartRecording(100, 24);
        var start = AnsiConsole.ExportText().Length;

        new SpectreTerminalUi(() => 100, () => 24, () => selectedDate, () => now)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, TuiThemes.Wolf);
        var output = AnsiConsole.ExportText()[start..];

        output.Should().NotContain("06:17");
    }

    [Theory]
    [InlineData(70, 16)]
    [InlineData(80, 18)]
    [InlineData(100, 24)]
    [InlineData(140, 30)]
    public void ShowPlanner_keeps_the_live_time_marker_inside_the_responsive_height_budget(
        int width,
        int height)
    {
        var date = new DateOnly(2026, 7, 15);
        var now = new DateTime(2026, 7, 15, 6, 17, 0);
        var state = PlannerState.CreateInitial(date) with { SlotIndex = 1 };
        var view = new DayPlannerPresenter().CreateView(new ProjectCatalog([], []), state);
        StartRecording(width, height);
        var start = AnsiConsole.ExportText().Length;

        new SpectreTerminalUi(() => width, () => height, () => date, () => now)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, TuiThemes.Wolf);
        var lines = AnsiConsole.ExportText()[start..]
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(height - 1);
        lines[0].Should().Contain("[TODOS]");
        lines.Should().Contain(line => line.Contains("06:17", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowPlanner_renders_responsive_details_for_the_selected_assignment()
    {
        var date = new DateOnly(2026, 7, 15);
        var todo = new TodoItem(
            1,
            false,
            "EXT-42",
            "Prepare proposal",
            TodoPriority.High,
            ["client"],
            null,
            new DateOnly(2026, 7, 16),
            "Work",
            [new TodoNote(2, "Check the numbers")],
            [])
        {
            Schedule = new TodoSchedule(date, new TimeOnly(6, 0))
        };
        var catalog = new ProjectCatalog(
            [new TodoProject("Consulting", "/todos/work.md", [todo])],
            []);
        var view = new DayPlannerPresenter().CreateView(catalog, PlannerState.CreateInitial(date));

        StartRecording(140, 30);
        var start = AnsiConsole.ExportText().Length;
        new SpectreTerminalUi(() => 140, () => 30)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, TuiThemes.Wolf);
        var wide = AnsiConsole.ExportText()[start..];

        StartRecording(100, 24);
        start = AnsiConsole.ExportText().Length;
        new SpectreTerminalUi(() => 100, () => 24)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, TuiThemes.Wolf);
        var narrow = AnsiConsole.ExportText()[start..];

        wide.Should().Contain("INSPECTOR")
            .And.Contain("Prepare proposal")
            .And.Contain("PROJECT: Consulting")
            .And.Contain("Check the numbers");
        narrow.Should().Contain("SELECTED")
            .And.Contain("Prepare proposal")
            .And.Contain("Consulting");
    }

    [Fact]
    public void ShowPlanner_uses_browser_semantic_colors_for_todos_and_prominent_panels()
    {
        var date = new DateOnly(2026, 7, 15);
        var timed = new TodoItem(
            1, false, "REF-TIMED", "Timed task", TodoPriority.High, ["timed"], null, null, string.Empty, [], [])
        {
            Schedule = new TodoSchedule(date, new TimeOnly(6, 0))
        };
        var allDay = new TodoItem(
            2, false, "REF-ALLDAY", "All-day task", TodoPriority.Highest, ["allday"], null, null, string.Empty, [], [])
        {
            Schedule = new TodoSchedule(date)
        };
        var theme = TuiThemes.Wolf with
        {
            Accent = new Color(1, 2, 3),
            AccentBright = new Color(4, 5, 6),
            Error = new Color(7, 8, 9),
            Warning = new Color(10, 11, 12),
            Info = new Color(13, 14, 15),
            Tag = new Color(16, 17, 18),
            Date = new Color(19, 20, 21),
            SecondaryText = new Color(22, 23, 24),
            BorderActive = new Color(25, 26, 27)
        };
        var catalog = new ProjectCatalog([new TodoProject("Work", "/todos/work.md", [timed, allDay])], []);
        var view = new DayPlannerPresenter().CreateView(catalog, PlannerState.CreateInitial(date));
        StartRecording(140, 30);
        var textStart = AnsiConsole.ExportText().Length;

        new SpectreTerminalUi(() => 140, () => 30)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, theme);
        var html = NormalizeHtml(AnsiConsole.ExportHtml());
        var text = AnsiConsole.ExportText()[textStart..];

        text.Should().Contain("REF-TIMED - Timed task  [Work] #timed")
            .And.Contain("REF-ALLDAY - All-day task  [Work]")
            .And.Contain("#allday")
            .And.Contain("INSPECTOR")
            .And.Contain("ALL DAY");
        html.Should().Contain("#040506")
            .And.Contain("#070809")
            .And.Contain("#0d0e0f")
            .And.Contain("#101112")
            .And.Contain("#131415")
            .And.Contain("#161718")
            .And.Contain("#191a1b");
    }

    [Fact]
    public void ShowPlanner_renders_a_scrollable_multi_row_unscheduled_picker()
    {
        var date = new DateOnly(2026, 7, 15);
        var todos = Enumerable.Range(1, 5)
            .Select(index => new TodoItem(
                index, false, null, $"Candidate {index}", null, [], null, null, string.Empty, [], []))
            .ToImmutableArray();
        var catalog = new ProjectCatalog(
            [new TodoProject("Work", "/todos/work.md", todos)],
            []);
        var state = PlannerState.CreateInitial(date) with
        {
            Mode = PlannerMode.ChooseTodo,
            PickerIndex = 4
        };
        var view = new DayPlannerPresenter().CreateView(catalog, state);
        StartRecording(100, 24);
        var start = AnsiConsole.ExportText().Length;

        new SpectreTerminalUi(() => 100, () => 24)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, TuiThemes.Wolf);
        var output = AnsiConsole.ExportText()[start..];

        output.Should().Contain("UNSCHEDULED TODOS")
            .And.Contain("Candidate 2")
            .And.Contain("Candidate 4")
            .And.Contain("> Candidate 5");
    }

    [Fact]
    public void ShowPlanner_keeps_the_full_todo_form_and_tabs_inside_the_viewport()
    {
        var date = new DateOnly(2026, 7, 15);
        var editor = TodoTaskEditorState.Create("/todos/work.md", true) with
        {
            Values = new TodoUpdate("Planned task", null, null, [], null, null)
        };
        var catalog = new ProjectCatalog(
            [new TodoProject("Work", "/todos/work.md", [])],
            []);
        var view = new DayPlannerPresenter().CreateView(
            catalog,
            PlannerState.CreateInitial(date) with { Editor = editor });
        StartRecording(100, 24);
        var start = AnsiConsole.ExportText().Length;

        new SpectreTerminalUi(() => 100, () => 24)
            .ShowPlanner(DefaultTabs, view, DefaultBindings, TuiThemes.Wolf);
        var lines = AnsiConsole.ExportText()[start..]
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCountLessThanOrEqualTo(23);
        lines[0].Should().Contain("[TODOS]");
        lines.Should().Contain(line => line.Contains("REFERENCE", StringComparison.Ordinal));
        lines.Should().Contain(line => line.Contains("SCHEDULED DATE", StringComparison.Ordinal));
        lines.Should().Contain(line => line.Contains("SCHEDULED TIME", StringComparison.Ordinal));
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
        var editor = TodoTaskEditorState.Edit(view.SelectedTodo!, identity) with
        {
            SelectedIndex = TodoTaskEditorState.FieldCount,
            Items =
            [
                new ContentNoteDraft(2, "Review current contract"),
                new ContentSubtaskDraft(3, "Request approval", false, 2)
            ]
        };
        var paletteState = CommandPaletteState.Closed with { IsOpen = true };
        var palette = new CommandPaletteView(
            paletteState,
            [new CommandPaletteItem(
                ApplicationActionId.BrowserEdit,
                "Todos",
                "Edit todo",
                "Open task editor",
                "e",
                true,
                null)]);
        StartRecording(100, 30);
        var terminal = new SpectreTerminalUi(() => 100, () => 30);

        terminal.ShowBrowser(
            DefaultTabs,
            view with { State = view.State with { Editor = editor } },
            DefaultBindings,
            TuiThemes.Wolf);
        terminal.ShowBrowser(
            DefaultTabs,
            view with
            {
                State = view.State with
                {
                    Editor = editor with
                    {
                        Mode = TodoTaskEditorMode.ChooseContentType,
                        AddKind = ContentItemKind.Subtask
                    }
                }
            },
            DefaultBindings,
            TuiThemes.Wolf);
        terminal.ShowBrowser(
            DefaultTabs,
            view with { CommandPalette = palette },
            DefaultBindings,
            TuiThemes.Wolf);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("EDIT TASK // Parent")
            .And.Contain("• Review current contract")
            .And.Contain("◯ Request approval  +2 nested")
            .And.Contain("ADD CONTENT")
            .And.Contain("> SUBTASK")
            .And.Contain("COMMAND PALETTE")
            .And.Contain("Edit todo");
    }

    [Theory]
    [InlineData(100, 30)]
    [InlineData(70, 16)]
    public void ShowBrowser_keeps_the_selected_content_item_in_the_responsive_outline(
        int width,
        int height)
    {
        var view = ViewWithTitle("Parent");
        var editor = TodoTaskEditorState.Edit(view.SelectedTodo!, view.SelectedTodoIdentity!) with
        {
            SelectedIndex = TodoTaskEditorState.FieldCount + 9,
            Items = [.. Enumerable.Range(1, 10)
                .Select(index => (ContentItemDraft)new ContentNoteDraft(index + 1, $"Content {index:00}"))]
        };

        var lines = RenderBrowser(
            view with { State = view.State with { Editor = editor } },
            width,
            height);

        lines.Should().HaveCount(height - 1);
        lines.Should().Contain(line => line.Contains("• Content 10", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_renders_fields_and_content_in_one_compact_editor()
    {
        var view = ViewWithTitle("Existing task");
        var editor = TodoTaskEditorState.Edit(view.SelectedTodo!, view.SelectedTodoIdentity!) with
        {
            Values = new TodoUpdate(
                "Renew contract",
                "EXT-42",
                null,
                ["work", "now"],
                new DateOnly(2026, 7, 15),
                new DateOnly(2026, 7, 31))
        };

        var lines = RenderBrowser(view with { State = view.State with { Editor = editor } }, 100, 24);
        var status = lines[StatusPanelTop(lines)..];
        var title = Array.FindIndex(status, line => line.Contains("TITLE", StringComparison.Ordinal));
        var reference = Array.FindIndex(status, line => line.Contains("REFERENCE", StringComparison.Ordinal));
        var priority = Array.FindIndex(status, line => line.Contains("PRIORITY", StringComparison.Ordinal));

        lines.Should().HaveCount(23);
        lines[0].Should().Contain("[TODOS]");
        status[title].Should().Contain("> ").And.Contain("Renew contract");
        status[reference].Should().Contain("EXT-42");
        status[priority].Should().Contain("—");
        status.Should().Contain(line => line.Contains("TAGS", StringComparison.Ordinal));
        status.Should().Contain(line => line.Contains("#work #now", StringComparison.Ordinal));
        status.Should().Contain(line => line.Contains("SCHEDULED DATE", StringComparison.Ordinal));
        status.Should().Contain(line => line.Contains("SCHEDULED TIME", StringComparison.Ordinal));
        status.Should().NotContain(line => line.Contains("START DATE", StringComparison.Ordinal));
        status.Should().NotContain(line => line.Contains("DUE DATE", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_applies_semantic_theme_roles_to_the_todo_form_only()
    {
        var view = ViewWithTitle("Existing task");
        var editor = TodoTaskEditorState.Edit(view.SelectedTodo!, view.SelectedTodoIdentity!) with
        {
            Values = new TodoUpdate("Active form value", "INACTIVE-42", null, [], null, null)
        };
        var theme = TuiThemes.Wolf with
        {
            Text = new Color(17, 17, 17),
            SecondaryText = new Color(17, 17, 17),
            Accent = new Color(34, 34, 34),
            AccentBright = new Color(34, 34, 34),
            Heading = new Color(51, 51, 51),
            Muted = new Color(68, 68, 68),
            Error = new Color(85, 85, 85),
            Border = new Color(102, 102, 102),
            BorderActive = new Color(102, 102, 102)
        };
        StartRecording(100, 30);
        var terminal = new SpectreTerminalUi(() => 100, () => 24);

        terminal.ShowBrowser(
            DefaultTabs,
            view with { State = view.State with { Editor = editor } },
            DefaultBindings,
            theme);
        var formHtml = NormalizeHtml(AnsiConsole.ExportHtml());

        StartRecording(100, 30);
        terminal.ShowBrowser(
            DefaultTabs,
            view with
            {
                State = view.State with
                {
                    Editor = editor with { Error = "Theme validation error" }
                }
            },
            DefaultBindings,
            theme);
        var errorHtml = NormalizeHtml(AnsiConsole.ExportHtml());

        StartRecording(100, 24);
        terminal.ShowBrowser(
            DefaultTabs,
            view with
            {
                State = view.State with { IsFilterMode = true, FilterDraft = "unique-filter" }
            },
            DefaultBindings,
            theme);
        var filterHtml = NormalizeHtml(AnsiConsole.ExportHtml());
        StyleBefore(formHtml, "content").Should().Contain("#333333").And.Contain("font-weight: bold");
        StyleBefore(formHtml, "inactive-42").Should().Contain("#111111");
        StyleBefore(formHtml, "title").Should().Contain("#222222").And.Contain("font-weight: bold");
        StyleBefore(formHtml, "—").Should().Contain("#2d343b").And.Contain("#162433");
        StyleBefore(formHtml, "j/k").Should().Contain("#2d343b").And.Contain("#162433");
        StyleBefore(errorHtml, "theme").Should().Contain("#555555").And.Contain("font-weight: bold");
        StyleBefore(filterHtml, "unique-filter").Should().Contain("#222222");
        formHtml.Should().Contain("#666666");
    }

    [Fact]
    public void ShowBrowser_renders_only_the_active_editing_field_in_the_compact_todo_form()
    {
        var view = ViewWithTitle("Existing task");
        var editor = TodoTaskEditorState.Edit(view.SelectedTodo!, view.SelectedTodoIdentity!) with
        {
            SelectedIndex = (int)TodoFormField.Reference,
            Mode = TodoTaskEditorMode.Edit,
            Draft = new string('x', 100),
            Values = new TodoUpdate("Renew contract", null, null, [], null, null)
        };

        var lines = RenderBrowser(view with { State = view.State with { Editor = editor } }, 70, 16);
        var status = lines[StatusPanelTop(lines)..];

        lines.Should().HaveCount(15);
        lines[0].Should().Contain("[TODOS]");
        status.Should().Contain(line => line.Contains("REFERENCE", StringComparison.Ordinal));
        status.Should().Contain(line => line.Contains("> REFERENCE", StringComparison.Ordinal) &&
            line.Contains("…", StringComparison.Ordinal));
        status.Should().NotContain(line => line.Contains("START DATE", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_replaces_form_hints_with_the_validation_error()
    {
        var view = ViewWithTitle("Existing task");
        var editor = TodoTaskEditorState.Edit(view.SelectedTodo!, view.SelectedTodoIdentity!) with
        {
            Mode = TodoTaskEditorMode.Edit,
            Draft = string.Empty,
            Values = new TodoUpdate(string.Empty, null, null, [], null, null),
            Error = "Title is required."
        };

        var lines = RenderBrowser(view with { State = view.State with { Editor = editor } }, 70, 16);
        var status = lines[StatusPanelTop(lines)..];

        status.Should().Contain(line => line.Contains("> TITLE", StringComparison.Ordinal) &&
            line.Contains("_", StringComparison.Ordinal));
        status.Should().Contain(line => line.Contains("Title is required.", StringComparison.Ordinal));
        status.Should().NotContain(line => line.Contains("Ctrl+S SAVE", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(70, 16)]
    [InlineData(80, 18)]
    [InlineData(100, 24)]
    [InlineData(120, 24)]
    [InlineData(140, 30)]
    public void ShowPlanner_leaves_the_final_terminal_row_free_and_keeps_tabs_visible(
        int width,
        int height)
    {
        var lines = RenderPlanner(width, height);

        lines.Should().HaveCount(height - 1);
        lines[0].Should().Contain("[DAY PLANNER]");
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
        lines[0].Should().Contain("[DAY PLANNER]");
    }

    [Fact]
    public void ShowBrowser_keeps_wide_column_boundaries_when_details_wrap()
    {
        var shortView = ViewWithTitle("Short title");
        var longView = ViewWithTitle(new string('x', 160));

        var shortHeader = RenderHeader(shortView);
        var longHeader = RenderHeader(longView);

        longHeader.IndexOf("TODOS:", StringComparison.Ordinal)
            .Should().Be(shortHeader.IndexOf("TODOS:", StringComparison.Ordinal));
        longHeader.IndexOf("DETAILS", StringComparison.Ordinal)
            .Should().Be(shortHeader.IndexOf("DETAILS", StringComparison.Ordinal));
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
            [])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 30))
        };
        var view = new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, [], true, new TodoIdentity("/todos/contracts.md", todo.SourceLine))],
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

        todoPane.Should().Contain("◯ H Prepare the unusually").And.Contain("…");
        todoPane.Should().NotContain("134416").And.NotContain("#now").And.NotContain("2026-07-08");
        output.Should().Contain("quarterly review")
            .And.Contain("meeting")
            .And.Contain("REFERENCE: 134416")
            .And.Contain("TAGS: #now")
            .And.Contain("SCHEDULED: 2026-07-15 09:30");
        output.Should().NotContain("START: 2026-07-08").And.NotContain("DUE: 2026-07-12");
    }

    [Fact]
    public void ShowBrowser_renders_priority_between_status_and_title_for_todos_and_subtasks()
    {
        var priorities = new[]
        {
            (TodoPriority.Highest, "!"),
            (TodoPriority.High, "H"),
            (TodoPriority.Medium, "M"),
            (TodoPriority.Low, "L"),
            (TodoPriority.Lowest, ".")
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
            [.. todos.Select((todo, index) => new TodoRow(null, todo, [], index == 0))],
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
            output.Should().Contain($"◯ {marker} Priority {priority}");
        }

        output.Should().Contain("✓ M Nested task");
    }

    [Theory]
    [InlineData(140, 30)]
    [InlineData(100, 20)]
    [InlineData(70, 16)]
    public void ShowBrowser_renders_schedules_in_the_adaptive_task_column(
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
            [
                new TodoRow(null, scheduled, [], true),
                new TodoRow(null, nested, [TodoTreeSegment.LastSibling], false)
            ],
            scheduled,
            "All",
            "/todos/project.md",
            null,
            string.Empty);

        var lines = RenderBrowser(view, width, height);
        var scheduledTitle = Array.FindIndex(lines, line => line.Contains("◯ H Prepare proposal", StringComparison.Ordinal));
        var nestedTitle = Array.FindIndex(lines, line => line.Contains("Nested follow-up", StringComparison.Ordinal));

        scheduledTitle.Should().BeGreaterThanOrEqualTo(0);
        lines[scheduledTitle].Should().Contain("2026-07-15 09:30");
        nestedTitle.Should().BeGreaterThanOrEqualTo(0);
        lines[nestedTitle].Should().Contain("✓ - └─ Nested follow-up");
        lines[nestedTitle].Should().Contain("2026-07-16 10:00");
    }

    [Theory]
    [InlineData(140, 30)]
    [InlineData(100, 20)]
    [InlineData(70, 16)]
    public void ShowBrowser_renders_tags_beneath_their_task_titles(
        int width,
        int height)
    {
        var root = new TodoItem(
            1, false, null, "Prepare proposal", TodoPriority.High, ["work", "now"],
            null, null, string.Empty, [], []);
        var nested = new TodoItem(
            2, true, null, "Nested follow-up", null, ["client"],
            null, null, string.Empty, [], []);
        var view = new BrowserView(
            BrowserState.Initial with { Focus = BrowserFocus.Todos },
            [new ProjectRow("All", 2, null, null, true)],
            [
                new TodoRow(null, root, [], true),
                new TodoRow(null, nested, [TodoTreeSegment.LastSibling], false)
            ],
            root,
            "All",
            "/todos/project.md",
            null,
            string.Empty);

        var lines = RenderBrowser(view, width, height);
        var rootTitle = Array.FindIndex(lines, line =>
            line.Contains("> ◯ H Prepare proposal", StringComparison.Ordinal));
        var nestedTitle = Array.FindIndex(lines, line =>
            line.Contains("✓ - └─ Nested follow-up", StringComparison.Ordinal));

        rootTitle.Should().BeGreaterThanOrEqualTo(0);
        nestedTitle.Should().BeGreaterThanOrEqualTo(0);
        lines[rootTitle + 1].Should().Contain("#work #now");
        lines[nestedTitle + 1].Should().Contain("#client");
        lines[rootTitle + 1].IndexOf("#work", StringComparison.Ordinal)
            .Should().Be(lines[rootTitle].IndexOf("Prepare proposal", StringComparison.Ordinal));
        lines[nestedTitle + 1].IndexOf("#client", StringComparison.Ordinal)
            .Should().Be(lines[nestedTitle].IndexOf("Nested follow-up", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_truncates_tags_inside_the_adaptive_task_column()
    {
        var todo = new TodoItem(
            1,
            false,
            null,
            "Short title",
            null,
            ["this-is-a-very-long-tag-that-cannot-fit-in-the-task-column"],
            null,
            null,
            string.Empty,
            [],
            []);
        var view = new BrowserView(
            BrowserState.Initial with { Focus = BrowserFocus.Todos },
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, [], true) { ProjectTitle = "DistinctProject" }],
            null,
            "All",
            "/todos/project.md",
            null,
            string.Empty);

        var lines = RenderBrowser(view, 140, 30);
        var tagLine = lines.Single(line => line.Contains("#this-is-a-very", StringComparison.Ordinal));
        var todoPane = tagLine.Split('│')[2];

        todoPane.Should().Contain("…");
        todoPane.Should().NotContain("DistinctProject").And.NotContain("2026-");
    }

    [Fact]
    public void ShowBrowser_applies_tag_selection_theme_to_the_browser_tag_line()
    {
        var todo = new TodoItem(
            1, false, null, "Selected task", null, ["browser-tag"],
            null, null, string.Empty, [], []);
        var view = new BrowserView(
            BrowserState.Initial with { Focus = BrowserFocus.Todos },
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, [], true)],
            null,
            "All",
            "/todos/project.md",
            null,
            string.Empty);
        var theme = TuiThemes.Wolf with
        {
            AccentBright = new Color(1, 2, 3),
            Surface2 = new Color(4, 5, 6)
        };
        StartRecording();

        new SpectreTerminalUi(() => 140, () => 30)
            .ShowBrowser(DefaultTabs, view, DefaultBindings, theme);
        var html = NormalizeHtml(AnsiConsole.ExportHtml());

        StyleBefore(html, "#browser-tag").Should().Contain("#010203")
            .And.Contain("#040506")
            .And.Contain("font-weight: bold");
    }

    [Fact]
    public void ShowBrowser_renders_unicode_tree_connectors_in_the_list_and_inspector()
    {
        var grandchild = CreateTodoItem("Grandchild", 3);
        grandchild = grandchild with { Tags = ["deep"] };
        var firstChild = CreateTodoItem("First child", 2) with
        {
            Tags = ["branch"],
            Subtasks = [grandchild]
        };
        var lastChild = CreateTodoItem("Last child", 4) with { Tags = ["last"] };
        var parent = CreateTodoItem("Parent", 1) with { Subtasks = [firstChild, lastChild] };
        var view = new BrowserView(
            BrowserState.Initial with { Focus = BrowserFocus.Todos },
            [new ProjectRow("All", 4, null, null, true)],
            [
                new TodoRow(null, parent, [], true),
                new TodoRow(null, firstChild, [TodoTreeSegment.HasFollowingSibling], false),
                new TodoRow(
                    null,
                    grandchild,
                    [TodoTreeSegment.HasFollowingSibling, TodoTreeSegment.LastSibling],
                    false),
                new TodoRow(null, lastChild, [TodoTreeSegment.LastSibling], false)
            ],
            parent,
            "All",
            "/todos/project.md",
            null,
            string.Empty);

        var output = string.Join('\n', RenderBrowser(view, 140, 30));

        output.Should().Contain("├─ First child")
            .And.Contain("│  └─ Grandchild")
            .And.Contain("└─ Last child")
            .And.Contain("│  #branch")
            .And.Contain("│     #deep");
    }

    [Fact]
    public void ShowBrowser_renders_an_empty_schedule_value_for_an_unscheduled_todo()
    {
        var lines = RenderBrowser(ViewWithTitle("Unscheduled task"), 140, 30);

        lines.Should().Contain(line =>
            line.Contains("Unscheduled task", StringComparison.Ordinal) &&
            line.Contains("-", StringComparison.Ordinal));
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
        output.Should().Contain("FILTER: /renew").And.Contain("EMPTY Enter CLEARS");
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

        output.Should().Contain("SORT // n/N NAME").And.Contain("p/P PRIORITY")
            .And.Contain("t/T TAGS").And.Contain("o SOURCE");
        output.Should().Contain("t NAME↓").And.Contain("t PRIORITY↑");
    }

    [Fact]
    public void ShowBrowser_fits_the_multiline_sort_dialog_without_scrolling_the_tabs()
    {
        var view = ViewWithTodoCount(1, BrowserFocus.Todos, scheduled: true);
        var lines = RenderBrowser(
            view with { State = view.State with { IsSortMode = true } },
            40,
            16);

        lines[0].Should().Contain("[TODOS]");
        lines.Should().HaveCount(15);
        lines.Should().Contain(line => line.Contains("n/N NAME", StringComparison.Ordinal));
        lines.Should().Contain(line => line.Contains("p/P PRIORITY", StringComparison.Ordinal));
        lines.Should().Contain(line => line.Contains("Esc CANCEL", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_includes_the_filter_key_in_wide_and_compact_hints()
    {
        var view = ViewWithTitle("Renew contract");
        StartRecording();

        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view, DefaultBindings);
        new SpectreTerminalUi(() => 70, () => 16).ShowBrowser(DefaultTabs, view, DefaultBindings);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("/ FILTER  : COMMAND");
        output.Should().Contain("j/k MOVE").And.Contain("h/l BACK/OPEN");
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

        output.Should().Contain("n/p NAVIGATE")
            .And.Contain("Ctrl+F FILTER")
            .And.Contain(":done")
            .And.Contain(":quit");
        output.Should().NotContain("Ctrl+N");
    }

    [Fact]
    public void ShowBrowser_always_renders_the_selected_tab_strip()
    {
        StartRecording(140, 30);
        var existingOutputLength = AnsiConsole.ExportText().Length;
        new SpectreTerminalUi(
                () => 140,
                () => 30,
                () => new DateOnly(2030, 1, 2))
            .ShowBrowser(DefaultTabs, ViewWithTitle("Renew contract"), DefaultBindings);
        var output = AnsiConsole.ExportText()[existingOutputLength..]
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        output[0].Should().Contain("[TODOS]");
        output[0].Should().Contain("WED 02 JAN");
        output[0].Should().Contain("OPEN:1").And.Contain("FILES:CLEAN");
        output[0].Should().NotContain("TABS");
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

        output[0].Should().Contain("TODOS").And.Contain("[DAY PLANNER]");
        output[0].Should().Contain("L TABS");
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

        output.Should().NotContain(line => line.TrimStart('│', ' ').StartsWith("DETAILS", StringComparison.Ordinal));
        output.Should().Contain(line =>
            line.Contains("TODOS: ALL", StringComparison.Ordinal) ||
            line.Contains("TASKS // ALL", StringComparison.Ordinal));
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
        var selectedLine = Array.FindIndex(lines, line =>
            line.Contains("> ◯ - Todo 25", StringComparison.Ordinal));
        var firstTodoContent = Array.FindIndex(lines, line =>
            line.Contains("Todo ", StringComparison.Ordinal) || line.Contains("⏳", StringComparison.Ordinal));

        lines.Should().HaveCount(23);
        selectedLine.Should().BeGreaterThanOrEqualTo(0);
        lines[selectedLine].Should().Contain("2026-07-15 09:30");
        lines[firstTodoContent].Should().Contain("Todo ");
    }

    [Fact]
    public void ShowBrowser_keeps_a_selected_todo_and_its_tags_together_in_the_visible_window()
    {
        var lines = RenderBrowser(
            ViewWithTodoCount(25, BrowserFocus.Todos, selectedIndex: 24, tagged: true),
            140,
            24);
        var selectedLine = Array.FindIndex(lines, line =>
            line.Contains("> ◯ - Todo 25", StringComparison.Ordinal));

        lines.Should().HaveCount(23);
        selectedLine.Should().BeGreaterThanOrEqualTo(0);
        lines[selectedLine + 1].Should().Contain("#focus");
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
            [new TodoRow(null, todo, [], true, new TodoIdentity("/todos/project.md", todo.SourceLine))],
            todo,
            "All",
            "/todos/project.md",
            null,
            string.Empty);
    }

    private static TodoItem CreateTodoItem(string title, int sourceLine) => new(
        sourceLine,
        false,
        null,
        title,
        null,
        [],
        null,
        null,
        string.Empty,
        [],
        []);

    private static BrowserView ViewWithTodoCount(
        int count,
        BrowserFocus focus,
        int selectedIndex = 0,
        bool scheduled = false,
        bool tagged = false)
    {
        var todos = Enumerable.Range(1, count)
            .Select(index => new TodoItem(
                index,
                false,
                null,
                $"Todo {index}",
                null,
                tagged ? ["focus"] : [],
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
        var rows = todos.Select((todo, index) => new TodoRow(null, todo, [], index == selectedIndex)).ToArray();

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
        Array.FindLastIndex(lines, line => line.StartsWith('┌'));

    private static string StyleBefore(string html, string text)
    {
        var textIndex = html.LastIndexOf(text, StringComparison.Ordinal);
        textIndex.Should().BeGreaterThanOrEqualTo(0);
        var spanIndex = html.LastIndexOf("<span", textIndex, StringComparison.Ordinal);
        spanIndex.Should().BeGreaterThanOrEqualTo(0);
        return html[spanIndex..textIndex];
    }

    private static string NormalizeHtml(string html) =>
        html.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();

    private static string RenderHeader(BrowserView view)
    {
        StartRecording();
        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(DefaultTabs, view, DefaultBindings);
        return AnsiConsole.ExportText()
            .Split(Environment.NewLine)
            .First(line => line.Contains("PROJECTS", StringComparison.Ordinal));
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
