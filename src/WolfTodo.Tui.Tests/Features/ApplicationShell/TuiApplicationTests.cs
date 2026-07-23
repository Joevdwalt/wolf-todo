using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class TuiApplicationTests
{
    [Fact]
    public void Run_reports_configuration_failure_and_returns_one()
    {
        var terminal = new FakeTerminal();
        var application = CreateApplication(new ThrowingConfigurationLoader(), terminal);

        var result = application.Run();

        result.Should().Be(1);
        terminal.StartupError.Should().Contain("missing");
        terminal.SplashShown.Should().BeFalse();
    }

    [Fact]
    public void Run_shows_the_todos_tab_and_browser_then_exits_for_the_configured_command()
    {
        var terminal = new FakeTerminal(Key('x'), Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal);

        var result = application.Run();

        result.Should().Be(0);
        terminal.SplashShown.Should().BeTrue();
        terminal.BrowserViews.Should().NotBeEmpty();
        terminal.TabViews.Should().OnlyContain(view =>
            view.Tabs.Length == 2 && view.Tabs[0].Title == "Todos" && view.Tabs[0].IsSelected &&
            view.Tabs[1].Title == "Day Planner");
        terminal.CursorVisibility.Should().Equal(false, true);
    }

    [Fact]
    public void Run_commits_and_clears_a_filter_before_exiting()
    {
        var terminal = new FakeTerminal(
            Key('x'),
            Key('/'),
            Key('m'),
            Key(ConsoleKey.Enter),
            Key('/'),
            Key(ConsoleKey.Backspace),
            Key(ConsoleKey.Enter),
            Key(':'),
            Key('q'),
            Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal);

        var result = application.Run();

        result.Should().Be(0);
        terminal.BrowserViews.Should().Contain(view => view.State.FilterText == "m");
        terminal.BrowserViews.Last().State.FilterText.Should().BeEmpty();
    }

    [Fact]
    public void Run_passes_resolved_key_bindings_to_the_terminal()
    {
        var bindings = TuiKeyBindings.CreateDefaults(":q") with
        {
            MoveDown = [KeyGesture.Parse("n")]
        };
        var terminal = new FakeTerminal(Key('x'), Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(bindings), terminal);

        application.Run();

        terminal.KeyBindings.Should().OnlyContain(candidate => candidate == bindings);
        terminal.Themes.Should().OnlyContain(candidate => candidate == TuiThemes.Wolf);
    }

    [Fact]
    public void Run_switches_to_the_day_planner_and_back_without_resetting_the_shell()
    {
        var terminal = new FakeTerminal(
            Key('x'),
            Key('L'),
            Key('L'),
            Key(':'),
            Key('q'),
            Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal);

        var result = application.Run();

        result.Should().Be(0);
        terminal.PlannerViews.Should().ContainSingle();
        terminal.PlannerViews.Single().State.SelectedDate.Should().Be(DateOnly.FromDateTime(DateTime.Today));
        terminal.BrowserViews.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void Run_exits_for_the_global_quit_command_from_the_day_planner()
    {
        var terminal = new FakeTerminal(
            Key('x'),
            Key('L'),
            Key(':'),
            Key('q'),
            Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal);

        var result = application.Run();

        result.Should().Be(0);
        terminal.PlannerViews.Should().NotBeEmpty();
        terminal.PlannerViews.Should().Contain(view => view.GlobalCommand == ":q");
    }

    [Fact]
    public void Run_redraws_the_day_planner_after_an_idle_input_timeout_without_changing_state()
    {
        var terminal = new FakeTerminal(
            Key('x'),
            Key('L'),
            Key(':'),
            Key('q'),
            Key(ConsoleKey.Enter))
        {
            TimeoutNextTimedRead = true
        };
        var application = CreateApplication(new FixedConfigurationLoader(), terminal);

        var result = application.Run();

        result.Should().Be(0);
        terminal.TimedReadCount.Should().BeGreaterThan(1);
        terminal.PlannerViews.Should().HaveCountGreaterThan(2);
        terminal.PlannerViews[1].State.Should().Be(terminal.PlannerViews[0].State);
    }

    [Fact]
    public void Run_applies_the_global_completed_command_from_the_day_planner()
    {
        var terminal = new FakeTerminal(
            Key('x'),
            Key('L'),
            Key(':'),
            Key('c'), Key('o'), Key('m'), Key('p'), Key('l'), Key('e'), Key('t'), Key('e'), Key('d'),
            Key(ConsoleKey.Enter),
            Key('H'),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal);

        application.Run();

        terminal.BrowserViews.Last().State.ShowCompleted.Should().BeTrue();
    }

    [Fact]
    public void Run_completes_and_executes_roll_today_for_the_selected_project()
    {
        var today = new DateOnly(2026, 7, 23);
        var fileSystem = new MutableProjectFileSystem(
            "/todos/project.md",
            "# Work\n\n- [ ] Timed ⏰ 09:30 ⏳ 2026-07-20\n- [x] Done ⏳ 2026-07-19\n");
        var terminal = new FakeTerminal(
            Key('x'),
            Key(':'), Key('r'), Key('o'), Key('l'), Key('l'),
            Key(ConsoleKey.Tab), Key(ConsoleKey.Enter),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            new FakeApplicationStateStore("/todos/project.md"),
            projectFileSystem: fileSystem,
            todayProvider: () => today);

        var result = application.Run();

        result.Should().Be(0);
        terminal.BrowserViews.Should().Contain(view => view.GlobalCommand == ":roll-today");
        fileSystem.Contents.Should().Contain("Timed ⏰ 09:30 ⏳ 2026-07-23");
        fileSystem.Contents.Should().Contain("Done ⏳ 2026-07-19");
    }

    [Fact]
    public void Run_opens_the_global_palette_and_executes_its_selected_action()
    {
        var terminal = new FakeTerminal(Key('x'), Key('?'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal);

        var result = application.Run();

        result.Should().Be(0);
        terminal.BrowserViews.Should().Contain(view => view.CommandPalette != null);
        terminal.BrowserViews
            .First(view => view.CommandPalette is not null)
            .CommandPalette!.SelectedItem!.Action.Should().Be(ApplicationActionId.Exit);
    }

    [Fact]
    public void Run_restores_the_project_matching_the_saved_path()
    {
        var stateStore = new FakeApplicationStateStore("/todos/project.md");
        var terminal = new FakeTerminal(Key('x'), Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal, stateStore);

        application.Run();

        terminal.BrowserViews.First().SelectedProjectPath.Should().Be("/todos/project.md");
        terminal.BrowserViews.First().State.Focus.Should().Be(BrowserFocus.Todos);
    }

    [Fact]
    public void Run_falls_back_to_all_when_the_saved_project_is_not_configured()
    {
        var savedSort = new TodoSort(TodoSortProperty.Schedule, TodoSortDirection.Descending);
        var stateStore = new FakeApplicationStateStore("/todos/removed.md", savedSort);
        var terminal = new FakeTerminal(Key('x'), Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal, stateStore);

        application.Run();

        terminal.BrowserViews.First().SelectedProjectTitle.Should().Be("All");
        terminal.BrowserViews.First().SelectedProjectPath.Should().BeNull();
        terminal.BrowserViews.First().State.Sort.Should().Be(savedSort);
    }

    [Fact]
    public void Run_saves_the_selected_project_when_exiting()
    {
        var stateStore = new FakeApplicationStateStore(null);
        var terminal = new FakeTerminal(
            Key('x'),
            new ConsoleKeyInfo('\0', ConsoleKey.Tab, shift: true, alt: false, control: false),
            Key('j'),
            Key('j'),
            Key(':'),
            Key('q'),
            Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal, stateStore);

        application.Run();

        stateStore.SavedProjectPath.Should().Be("/todos/project.md");
    }

    [Fact]
    public void Run_does_not_persist_today_as_a_project_selection()
    {
        var stateStore = new FakeApplicationStateStore(null);
        var terminal = new FakeTerminal(
            Key('x'),
            new ConsoleKeyInfo('\0', ConsoleKey.Tab, shift: true, alt: false, control: false),
            Key('j'),
            Key(':'),
            Key('q'),
            Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal, stateStore);

        application.Run();

        terminal.BrowserViews.Should().Contain(view => view.SelectedProjectTitle == "@today");
        stateStore.SavedProjectPath.Should().BeNull();
    }

    [Fact]
    public void Run_restores_sorting_and_saves_it_when_exiting_from_planner()
    {
        var savedSort = new TodoSort(TodoSortProperty.Tags, TodoSortDirection.Ascending);
        var stateStore = new FakeApplicationStateStore("/todos/project.md", savedSort);
        var terminal = new FakeTerminal(
            Key('x'),
            Key('t'),
            Key('N'),
            Key('L'),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal, stateStore);

        application.Run();

        terminal.BrowserViews.First().State.Sort.Should().Be(savedSort);
        terminal.PlannerViews.Should().NotBeEmpty();
        stateStore.SavedState!.SelectedProjectPath.Should().Be("/todos/project.md");
        stateStore.SavedState.Sort.Should().Be(
            new TodoSort(TodoSortProperty.Name, TodoSortDirection.Descending));
    }

    [Fact]
    public void Run_suspends_for_external_editing_and_reloads_the_selected_project()
    {
        var fileSystem = new CountingProjectFileSystem(
            "/todos/project.md",
            "# Work\n\n- [ ] First\n- [ ] Selected\n");
        var launcher = new FakeExternalEditorLauncher(ExternalEditorResult.Success);
        var stateStore = new FakeApplicationStateStore(
            "/todos/project.md",
            new TodoSort(TodoSortProperty.Priority, TodoSortDirection.Descending));
        var terminal = new FakeTerminal(
            Key('x'),
            Key('j'),
            Key(ConsoleKey.E, control: true),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            stateStore,
            launcher,
            fileSystem);

        var result = application.Run();

        result.Should().Be(0);
        launcher.Calls.Should().ContainSingle().Which.Should().Be(("/todos/project.md", 4));
        terminal.ExternalSuspensions.Should().Be(1);
        terminal.ExternalResumptions.Should().Be(1);
        fileSystem.ReadCount.Should().BeGreaterThan(1);
        terminal.BrowserViews.Last().SelectedTodo!.Title.Should().Be("Selected");
        terminal.BrowserViews.Last().State.Sort.Should().Be(
            new TodoSort(TodoSortProperty.Priority, TodoSortDirection.Descending));
    }

    [Fact]
    public void Run_shows_external_editor_failures_without_exiting()
    {
        var fileSystem = new CountingProjectFileSystem(
            "/todos/project.md",
            "# Work\n\n- [ ] Selected\n");
        var launcher = new FakeExternalEditorLauncher(
            ExternalEditorResult.Failure(false, "$EDITOR is not configured."));
        var terminal = new FakeTerminal(
            Key('x'),
            Key(ConsoleKey.E, control: true),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            new FakeApplicationStateStore("/todos/project.md"),
            launcher,
            fileSystem);

        var result = application.Run();

        result.Should().Be(0);
        terminal.BrowserViews.Should().Contain(view =>
            view.State.Error == "$EDITOR is not configured.");
        fileSystem.ReadCount.Should().Be(1);
    }

    [Fact]
    public void Run_creates_a_todo_with_the_selected_planner_schedule_using_the_full_form()
    {
        var fileSystem = new MutableProjectFileSystem(
            "/todos/project.md",
            "# Work\n\n## Inbox\n");
        var terminal = new FakeTerminal(
            Key('x'),
            Key('L'),
            Key('a'),
            Key(ConsoleKey.Enter),
            Key(ConsoleKey.Enter),
            Key('P'), Key('l'), Key('a'), Key('n'), Key('n'), Key('e'), Key('d'),
            Key(ConsoleKey.Enter),
            Key(ConsoleKey.S, control: true),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain("- [ ] Planned")
            .And.Contain($"⏰ 06:00 ⏱ 30m ⏳ {DateOnly.FromDateTime(DateTime.Today):yyyy-MM-dd}");
        terminal.PlannerViews.Should().Contain(view => view.State.Editor != null);
        terminal.PlannerViews.Last().SelectedAssignment!.Todo.Title.Should().Be("Planned");
    }

    [Fact]
    public void Run_uses_completion_and_external_editor_actions_from_the_planner()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var fileSystem = new MutableProjectFileSystem(
            "/todos/project.md",
            $"# Work\n\n- [ ] Scheduled ⏳ {today:yyyy-MM-dd} ⏰ 06:00\n");
        var launcher = new FakeExternalEditorLauncher(ExternalEditorResult.Success);
        var terminal = new FakeTerminal(
            Key('x'),
            Key('L'),
            Key(ConsoleKey.Spacebar),
            Key(ConsoleKey.E, control: true),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            externalEditorLauncher: launcher,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain("- [x] Scheduled");
        launcher.Calls.Should().ContainSingle().Which.Should().Be(("/todos/project.md", 3));
        terminal.ExternalSuspensions.Should().Be(1);
        terminal.ExternalResumptions.Should().Be(1);
    }

    [Fact]
    public void Run_keeps_the_planner_form_open_when_the_create_write_fails()
    {
        var fileSystem = new FailingWriteProjectFileSystem(
            "/todos/project.md",
            "# Work\n\n## Inbox\n");
        var terminal = new FakeTerminal(
            Key('x'), Key('L'), Key('a'), Key(ConsoleKey.Enter), Key(ConsoleKey.Enter),
            Key('T'), Key('a'), Key('s'), Key('k'), Key(ConsoleKey.Enter),
            Key(ConsoleKey.S, control: true),
            Key('h'),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        terminal.PlannerViews.Should().Contain(view =>
            view.State.Editor != null &&
            view.State.Editor.Error != null &&
            view.State.Editor.Error.Contains("disk full", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_follows_a_todo_to_its_new_slot_after_planner_rescheduling()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);
        var fileSystem = new MutableProjectFileSystem(
            "/todos/project.md",
            $"# Work\n\n- [ ] Scheduled ⏳ {today:yyyy-MM-dd} ⏰ 06:00\n");
        var keys = new List<ConsoleKeyInfo>
        {
            Key('x'), Key('L'), Key('e'),
            Key('j'), Key('j'), Key('j'), Key('j'), Key('l')
        };
        keys.AddRange(Enumerable.Repeat(Key(ConsoleKey.Backspace), 10));
        keys.AddRange(tomorrow.ToString("yyyy-MM-dd").Select(Key));
        keys.Add(Key(ConsoleKey.Enter));
        keys.AddRange([Key('j'), Key('l')]);
        keys.AddRange(Enumerable.Repeat(Key(ConsoleKey.Backspace), 5));
        keys.AddRange("07:30".Select(Key));
        keys.AddRange([
            Key(ConsoleKey.Enter),
            Key(ConsoleKey.S, control: true),
            Key(':'), Key('q'), Key(ConsoleKey.Enter)
        ]);
        var terminal = new FakeTerminal([.. keys]);
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain($"⏰ 07:30 ⏳ {tomorrow:yyyy-MM-dd}");
        var final = terminal.PlannerViews.Last();
        final.State.SelectedDate.Should().Be(tomorrow);
        final.State.SlotIndex.Should().Be(6);
        final.SelectedAssignment!.Todo.Title.Should().Be("Scheduled");
    }

    [Fact]
    public void Run_allows_an_overlapping_schedule_from_the_browser_form()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var original =
            $"# Work\n\n- [ ] Unscheduled\n- [ ] Occupied ⏳ {date:yyyy-MM-dd} ⏰ 09:30\n";
        var fileSystem = new MutableProjectFileSystem("/todos/project.md", original);
        var keys = new List<ConsoleKeyInfo>
        {
            Key('x'), Key('e'),
            Key('j'), Key('j'), Key('j'), Key('j'), Key('l')
        };
        keys.AddRange(date.ToString("yyyy-MM-dd").Select(Key));
        keys.AddRange([Key(ConsoleKey.Enter), Key('j'), Key('l')]);
        keys.AddRange("09:30".Select(Key));
        keys.AddRange([
            Key(ConsoleKey.Enter),
            Key(ConsoleKey.S, control: true),
            Key('h'),
            Key(':'), Key('q'), Key(ConsoleKey.Enter)
        ]);
        var terminal = new FakeTerminal([.. keys]);
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain($"Unscheduled ⏰ 09:30 ⏳ {date:yyyy-MM-dd}");
    }

    [Fact]
    public void Run_allows_an_all_day_schedule_on_a_day_with_timed_todos()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var original =
            $"# Work\n\n- [ ] Unscheduled\n- [ ] Timed ⏳ {date:yyyy-MM-dd} ⏰ 09:30\n";
        var fileSystem = new MutableProjectFileSystem("/todos/project.md", original);
        var keys = new List<ConsoleKeyInfo>
        {
            Key('x'), Key('e'),
            Key('j'), Key('j'), Key('j'), Key('j'), Key('l')
        };
        keys.AddRange(date.ToString("yyyy-MM-dd").Select(Key));
        keys.AddRange([
            Key(ConsoleKey.Enter),
            Key(ConsoleKey.S, control: true),
            Key(':'), Key('q'), Key(ConsoleKey.Enter)
        ]);
        var terminal = new FakeTerminal([.. keys]);
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain($"- [ ] Unscheduled ⏳ {date:yyyy-MM-dd}")
            .And.NotContain("Unscheduled ⏰");
        terminal.BrowserViews.Any(view =>
            view.State.Editor is not null && view.State.Editor.Error == "That timeslot is already occupied.")
            .Should().BeFalse();
    }

    [Fact]
    public void Run_allows_changing_a_planned_todo_to_an_all_day_schedule()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var original =
            $"# Work\n\n- [ ] Planned ⏳ {date:yyyy-MM-dd} ⏰ 06:00\n- [ ] Timed ⏳ {date:yyyy-MM-dd} ⏰ 09:30\n";
        var fileSystem = new MutableProjectFileSystem("/todos/project.md", original);
        var keys = new List<ConsoleKeyInfo>
        {
            Key('x'), Key('L'), Key('e'),
            Key('j'), Key('j'), Key('j'), Key('j'), Key('j'), Key('l')
        };
        keys.AddRange(Enumerable.Repeat(Key(ConsoleKey.Backspace), 5));
        keys.AddRange([
            Key(ConsoleKey.Enter),
            Key(ConsoleKey.S, control: true),
            Key(':'), Key('q'), Key(ConsoleKey.Enter)
        ]);
        var terminal = new FakeTerminal([.. keys]);
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain($"- [ ] Planned ⏳ {date:yyyy-MM-dd}")
            .And.NotContain("Planned ⏰");
        terminal.PlannerViews.Any(view =>
            view.State.Editor is not null && view.State.Editor.Error == "That timeslot is already occupied.")
            .Should().BeFalse();
    }

    [Fact]
    public void Run_assigns_an_unscheduled_todo_to_all_day_from_the_planner_pane()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var fileSystem = new MutableProjectFileSystem(
            "/todos/project.md",
            "# Work\n\n- [ ] Available\n");
        var terminal = new FakeTerminal(
            Key('x'), Key('L'), Key(ConsoleKey.Tab),
            Key(ConsoleKey.Enter), Key(ConsoleKey.Enter),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain($"- [ ] Available ⏳ {date:yyyy-MM-dd}")
            .And.NotContain("Available ⏰");
        var final = terminal.PlannerViews.Last();
        final.State.Focus.Should().Be(PlannerFocus.AllDay);
        final.SelectedAllDayAssignment!.Todo.Title.Should().Be("Available");
    }

    [Fact]
    public void Run_creates_a_date_only_todo_from_the_all_day_pane()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var fileSystem = new MutableProjectFileSystem(
            "/todos/project.md",
            "# Work\n\n## Inbox\n");
        var terminal = new FakeTerminal(
            Key('x'), Key('L'), Key(ConsoleKey.Tab), Key('a'),
            Key(ConsoleKey.Enter), Key(ConsoleKey.Enter),
            Key('A'), Key('l'), Key('l'), Key('d'), Key('a'), Key('y'),
            Key(ConsoleKey.Enter), Key(ConsoleKey.S, control: true),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain($"- [ ] Allday ⏱ 30m ⏳ {date:yyyy-MM-dd}")
            .And.NotContain("Allday ⏰");
    }

    [Fact]
    public void Run_moves_a_timed_todo_to_all_day_without_clearing_its_duration()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var fileSystem = new MutableProjectFileSystem(
            "/todos/project.md",
            $"# Work\n\n- [ ] Deep work ⏰ 06:00 ⏱ 60m ⏳ {date:yyyy-MM-dd}\n");
        var terminal = new FakeTerminal(
            Key('x'), Key('L'), Key(ConsoleKey.Enter),
            Key(ConsoleKey.Tab), Key(ConsoleKey.Enter),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain($"Deep work ⏱ 60m ⏳ {date:yyyy-MM-dd}")
            .And.NotContain("Deep work ⏰");
    }

    [Fact]
    public void Run_moves_an_all_day_todo_to_a_timeline_slot_without_clearing_its_duration()
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var fileSystem = new MutableProjectFileSystem(
            "/todos/project.md",
            $"# Work\n\n- [ ] Deep work ⏱ 60m ⏳ {date:yyyy-MM-dd}\n");
        var terminal = new FakeTerminal(
            Key('x'), Key('L'), Key(ConsoleKey.Tab), Key(ConsoleKey.Enter),
            Key(ConsoleKey.Tab), Key('j'), Key(ConsoleKey.Enter),
            Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(
            new FixedConfigurationLoader(),
            terminal,
            projectFileSystem: fileSystem);

        application.Run();

        fileSystem.Contents.Should().Contain($"Deep work ⏰ 06:15 ⏱ 60m ⏳ {date:yyyy-MM-dd}");
    }

    private static TuiApplication CreateApplication(
        IApplicationConfigurationLoader configurationLoader,
        ITerminalUi terminal,
        IApplicationStateStore? applicationStateStore = null,
        IExternalEditorLauncher? externalEditorLauncher = null,
        IProjectFileSystem? projectFileSystem = null,
        Func<DateOnly>? todayProvider = null)
    {
        var fileSystem = projectFileSystem ?? new EmptyProjectFileSystem();
        var parser = new ProjectMarkdownParser();
        var catalogLoader = new ProjectCatalogLoader(fileSystem, parser);
        return new TuiApplication(
            configurationLoader,
            catalogLoader,
            terminal,
            applicationStateStore ?? new FakeApplicationStateStore(null),
            new ApplicationInputRouter(),
            new TabHostPresenter(),
            new TabHostReducer(),
            new ProjectBrowserPresenter(todayProvider),
            new BrowserReducer(todayProvider),
            "wolf",
            mutationService: new ProjectTodoMutationService(fileSystem, parser),
            externalEditorLauncher: externalEditorLauncher,
            todayProvider: todayProvider);
    }

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem1, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) =>
        new('\0', key, false, false, control);

    private sealed class FixedConfigurationLoader(TuiKeyBindings? bindings = null) : IApplicationConfigurationLoader
    {
        public ApplicationConfiguration Load() => new(
            ["/todos/project.md"],
            bindings ?? TuiKeyBindings.CreateDefaults(":q"));
    }

    private sealed class ThrowingConfigurationLoader : IApplicationConfigurationLoader
    {
        public ApplicationConfiguration Load() => throw new InvalidDataException("missing configuration");
    }

    private sealed class EmptyProjectFileSystem : IProjectFileSystem
    {
        public bool FileExists(string path) => false;

        public string GetFullPath(string path) => path;

        public string ReadAllText(string path) => throw new FileNotFoundException();
    }

    private sealed class CountingProjectFileSystem(string path, string contents) : IProjectFileSystem
    {
        public int ReadCount { get; private set; }

        public bool FileExists(string candidate) => candidate == path;

        public string GetFullPath(string candidate) => candidate;

        public string ReadAllText(string candidate)
        {
            ReadCount++;
            return contents;
        }
    }

    private sealed class MutableProjectFileSystem(string path, string contents) : IProjectFileSystem
    {
        public string Contents { get; private set; } = contents;

        public bool FileExists(string candidate) => candidate == path;

        public string GetFullPath(string candidate) => candidate;

        public string ReadAllText(string candidate) => Contents;

        public void WriteAllTextAtomically(string candidate, string updated) => Contents = updated;
    }

    private sealed class FailingWriteProjectFileSystem(string path, string contents) : IProjectFileSystem
    {
        public bool FileExists(string candidate) => candidate == path;

        public string GetFullPath(string candidate) => candidate;

        public string ReadAllText(string candidate) => contents;

        public void WriteAllTextAtomically(string candidate, string updated) =>
            throw new IOException("disk full");
    }

    private sealed class FakeExternalEditorLauncher(ExternalEditorResult result) : IExternalEditorLauncher
    {
        public List<(string ProjectPath, int SourceLine)> Calls { get; } = [];

        public ExternalEditorResult Open(string projectPath, int sourceLine)
        {
            Calls.Add((projectPath, sourceLine));
            return result;
        }
    }

    private sealed class FakeApplicationStateStore(
        string? selectedProjectPath,
        TodoSort? sort = null) : IApplicationStateStore
    {
        public ApplicationSessionState? SavedState { get; private set; }

        public string? SavedProjectPath => SavedState?.SelectedProjectPath;

        public ApplicationSessionState Load() => new(selectedProjectPath, sort ?? TodoSort.Source);

        public void Save(ApplicationSessionState state) => SavedState = state;
    }

    private sealed class FakeTerminal(params ConsoleKeyInfo[] keys) : ITerminalUi
    {
        private readonly Queue<ConsoleKeyInfo> keyQueue = new(keys);

        public List<BrowserView> BrowserViews { get; } = [];

        public List<PlannerView> PlannerViews { get; } = [];

        public List<TabStripView> TabViews { get; } = [];

        public List<TuiKeyBindings> KeyBindings { get; } = [];

        public List<TuiTheme> Themes { get; } = [];

        public string? StartupError { get; private set; }

        public bool SplashShown { get; private set; }

        public List<bool> CursorVisibility { get; } = [];

        public int ExternalSuspensions { get; private set; }

        public int ExternalResumptions { get; private set; }

        public bool TimeoutNextTimedRead { get; set; }

        public int TimedReadCount { get; private set; }

        public ConsoleKeyInfo ReadKey() => keyQueue.Dequeue();

        public ConsoleKeyInfo? ReadKey(TimeSpan timeout)
        {
            TimedReadCount++;
            if (TimeoutNextTimedRead)
            {
                TimeoutNextTimedRead = false;
                return null;
            }

            return keyQueue.Dequeue();
        }

        public void ShowBrowser(
            TabStripView tabs,
            BrowserView view,
            TuiKeyBindings keyBindings,
            TuiTheme theme)
        {
            TabViews.Add(tabs);
            BrowserViews.Add(view);
            KeyBindings.Add(keyBindings);
            Themes.Add(theme);
        }

        public void ShowPlanner(
            TabStripView tabs,
            PlannerView view,
            TuiKeyBindings keyBindings,
            TuiTheme theme)
        {
            TabViews.Add(tabs);
            PlannerViews.Add(view);
            KeyBindings.Add(keyBindings);
            Themes.Add(theme);
        }

        public void ShowSplash(string logo, TuiTheme theme)
        {
            SplashShown = true;
            Themes.Add(theme);
        }

        public void ShowStartupError(string message) => StartupError = message;

        public void SetCursorVisible(bool visible) => CursorVisibility.Add(visible);

        public void SuspendForExternalProcess() => ExternalSuspensions++;

        public void ResumeAfterExternalProcess() => ExternalResumptions++;
    }
}
