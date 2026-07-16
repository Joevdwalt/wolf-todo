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
        var savedSort = new TodoSort(TodoSortProperty.StartDate, TodoSortDirection.Descending);
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
            Key(':'),
            Key('q'),
            Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal, stateStore);

        application.Run();

        stateStore.SavedProjectPath.Should().Be("/todos/project.md");
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

    private static TuiApplication CreateApplication(
        IApplicationConfigurationLoader configurationLoader,
        ITerminalUi terminal,
        IApplicationStateStore? applicationStateStore = null,
        IExternalEditorLauncher? externalEditorLauncher = null,
        IProjectFileSystem? projectFileSystem = null)
    {
        var fileSystem = projectFileSystem ?? new EmptyProjectFileSystem();
        var catalogLoader = new ProjectCatalogLoader(fileSystem, new ProjectMarkdownParser());
        return new TuiApplication(
            configurationLoader,
            catalogLoader,
            terminal,
            applicationStateStore ?? new FakeApplicationStateStore(null),
            new ApplicationInputRouter(),
            new TabHostPresenter(),
            new TabHostReducer(),
            new ProjectBrowserPresenter(),
            new BrowserReducer(),
            "wolf",
            externalEditorLauncher: externalEditorLauncher);
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

        public ConsoleKeyInfo ReadKey() => keyQueue.Dequeue();

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
