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
    public void Run_restores_the_project_matching_the_saved_path()
    {
        var stateStore = new FakeApplicationStateStore("/todos/project.md");
        var terminal = new FakeTerminal(Key('x'), Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal, stateStore);

        application.Run();

        terminal.BrowserViews.First().SelectedProjectPath.Should().Be("/todos/project.md");
    }

    [Fact]
    public void Run_falls_back_to_all_when_the_saved_project_is_not_configured()
    {
        var stateStore = new FakeApplicationStateStore("/todos/removed.md");
        var terminal = new FakeTerminal(Key('x'), Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal, stateStore);

        application.Run();

        terminal.BrowserViews.First().SelectedProjectTitle.Should().Be("All");
        terminal.BrowserViews.First().SelectedProjectPath.Should().BeNull();
    }

    [Fact]
    public void Run_saves_the_selected_project_when_exiting()
    {
        var stateStore = new FakeApplicationStateStore(null);
        var terminal = new FakeTerminal(
            Key('x'),
            Key('j'),
            Key(':'),
            Key('q'),
            Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal, stateStore);

        application.Run();

        stateStore.SavedProjectPath.Should().Be("/todos/project.md");
    }

    private static TuiApplication CreateApplication(
        IApplicationConfigurationLoader configurationLoader,
        ITerminalUi terminal,
        IApplicationStateStore? applicationStateStore = null)
    {
        var fileSystem = new EmptyProjectFileSystem();
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
            "wolf");
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

    private sealed class FakeApplicationStateStore(string? selectedProjectPath) : IApplicationStateStore
    {
        public string? SavedProjectPath { get; private set; }

        public string? LoadSelectedProjectPath() => selectedProjectPath;

        public void SaveSelectedProjectPath(string? projectPath) => SavedProjectPath = projectPath;
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
    }
}
