using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

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
    public void Run_shows_the_browser_then_exits_for_the_configured_command()
    {
        var terminal = new FakeTerminal(Key('x'), Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(), terminal);

        var result = application.Run();

        result.Should().Be(0);
        terminal.SplashShown.Should().BeTrue();
        terminal.BrowserViews.Should().NotBeEmpty();
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
        var bindings = BrowserKeyBindings.CreateDefaults(":q") with
        {
            MoveDown = [KeyGesture.Parse("n")]
        };
        var terminal = new FakeTerminal(Key('x'), Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = CreateApplication(new FixedConfigurationLoader(bindings), terminal);

        application.Run();

        terminal.BrowserBindings.Should().OnlyContain(candidate => candidate == bindings);
    }

    private static TuiApplication CreateApplication(
        IApplicationConfigurationLoader configurationLoader,
        ITerminalUi terminal)
    {
        var fileSystem = new EmptyProjectFileSystem();
        var catalogLoader = new ProjectCatalogLoader(fileSystem, new ProjectMarkdownParser());
        return new TuiApplication(
            configurationLoader,
            catalogLoader,
            terminal,
            new ProjectBrowserPresenter(),
            new BrowserReducer(),
            "wolf");
    }

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem1, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private sealed class FixedConfigurationLoader(BrowserKeyBindings? bindings = null) : IApplicationConfigurationLoader
    {
        public ApplicationConfiguration Load() => new(
            ["/todos/project.md"],
            bindings ?? BrowserKeyBindings.CreateDefaults(":q"));
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

    private sealed class FakeTerminal(params ConsoleKeyInfo[] keys) : ITerminalUi
    {
        private readonly Queue<ConsoleKeyInfo> keyQueue = new(keys);

        public List<BrowserView> BrowserViews { get; } = [];

        public List<BrowserKeyBindings> BrowserBindings { get; } = [];

        public string? StartupError { get; private set; }

        public bool SplashShown { get; private set; }

        public ConsoleKeyInfo ReadKey() => keyQueue.Dequeue();

        public void ShowBrowser(BrowserView view, BrowserKeyBindings keyBindings)
        {
            BrowserViews.Add(view);
            BrowserBindings.Add(keyBindings);
        }

        public void ShowSplash(string logo) => SplashShown = true;

        public void ShowStartupError(string message) => StartupError = message;
    }
}
