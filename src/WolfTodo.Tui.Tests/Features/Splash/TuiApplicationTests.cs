using FluentAssertions;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Tests.Features.Splash;

public sealed class TuiApplicationTests
{
    [Fact]
    public void Run_reports_configuration_failure_and_returns_one()
    {
        var terminal = new FakeTerminal();
        var application = new TuiApplication(new ThrowingLoader(), terminal, new HomeScreenReducer(), "wolf");

        var result = application.Run();

        result.Should().Be(1);
        terminal.StartupError.Should().Contain("missing");
        terminal.SplashShown.Should().BeFalse();
    }

    [Fact]
    public void Run_shows_splash_then_exits_for_the_configured_command()
    {
        var terminal = new FakeTerminal(Key('x'), Key(':'), Key('q'), Key(ConsoleKey.Enter));
        var application = new TuiApplication(new FixedLoader(), terminal, new HomeScreenReducer(), "wolf");

        var result = application.Run();

        result.Should().Be(0);
        terminal.SplashShown.Should().BeTrue();
        terminal.HomeStates.Should().Contain(state => state.IsCommandMode && state.Command == ":q");
    }

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem1, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private sealed class FixedLoader : IKeybindingsLoader
    {
        public Keybindings Load() => new(":q");
    }

    private sealed class ThrowingLoader : IKeybindingsLoader
    {
        public Keybindings Load() => throw new InvalidDataException("missing keybindings");
    }

    private sealed class FakeTerminal(params ConsoleKeyInfo[] keys) : ITerminalUi
    {
        private readonly Queue<ConsoleKeyInfo> keyQueue = new(keys);

        public List<HomeScreenState> HomeStates { get; } = [];

        public string? StartupError { get; private set; }

        public bool SplashShown { get; private set; }

        public ConsoleKeyInfo ReadKey() => keyQueue.Dequeue();

        public void ShowHome(HomeScreenState state) => HomeStates.Add(state);

        public void ShowSplash(string logo) => SplashShown = true;

        public void ShowStartupError(string message) => StartupError = message;
    }
}
