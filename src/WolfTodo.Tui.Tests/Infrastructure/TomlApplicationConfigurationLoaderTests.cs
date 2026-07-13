using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class TomlApplicationConfigurationLoaderTests
{
    [Fact]
    public void Load_returns_directories_and_quit_command()
    {
        var path = Path.GetFullPath("todo.md");
        var loader = Loader($$"""
            [projects]
            files = ["{{path}}"]

            [keybindings]
            quit = ":quit"
            """);

        var result = loader.Load();

        result.ProjectFiles.Should().Equal(path);
        result.QuitCommand.Should().Be(":quit");
        result.KeyBindings.ToggleCompletedCommand.Should().Be(":completed");
        result.KeyBindings.MatchesMoveDown(Key('j')).Should().BeTrue();
        result.KeyBindings.MatchesMoveUp(Key(ConsoleKey.UpArrow)).Should().BeTrue();
    }

    [Fact]
    public void Load_replaces_only_explicitly_configured_bindings()
    {
        var path = Path.GetFullPath("todo.md");
        var loader = Loader($$"""
            [projects]
            files = ["{{path}}"]

            [keybindings]
            quit = ":quit"
            toggle_completed = ":done"
            move_down = ["n", "Ctrl+J"]
            """);

        var result = loader.Load();

        result.KeyBindings.ToggleCompletedCommand.Should().Be(":done");
        result.KeyBindings.MatchesMoveDown(Key('n')).Should().BeTrue();
        result.KeyBindings.MatchesMoveDown(Key('j')).Should().BeFalse();
        result.KeyBindings.MatchesMoveDown(Key(ConsoleKey.J, control: true)).Should().BeTrue();
        result.KeyBindings.MatchesMoveUp(Key('k')).Should().BeTrue();
    }

    [Theory]
    [InlineData("move_up = []", "*move_up*non-empty*")]
    [InlineData("move_up = [\"Meta+K\"]", "*move_up*Invalid modifier*")]
    [InlineData("move_up = [\"k\", \"k\"]", "*move_up*duplicate*")]
    [InlineData("move_up = [\"j\"]", "*both*move_up*move_down*")]
    [InlineData("toggle_completed = \":q\"", "*quit*toggle_completed*different*")]
    public void Load_rejects_invalid_or_conflicting_bindings(string binding, string expectedMessage)
    {
        var path = Path.GetFullPath("todo.md");
        var loader = Loader($$"""
            [projects]
            files = ["{{path}}"]

            [keybindings]
            quit = ":q"
            {{binding}}
            """);

        var action = loader.Invoking(candidate => candidate.Load());

        action.Should().Throw<InvalidDataException>().WithMessage(expectedMessage);
    }

    [Fact]
    public void Load_throws_when_a_project_file_is_relative()
    {
        var loader = Loader("""
            [projects]
            files = ["relative.md"]

            [keybindings]
            quit = ":q"
            """);

        var action = loader.Invoking(candidate => candidate.Load());

        action.Should().Throw<InvalidDataException>().WithMessage("*absolute*.md file path*");
    }

    [Fact]
    public void Load_throws_when_the_configuration_file_is_missing()
    {
        var loader = new TomlApplicationConfigurationLoader("config.toml", candidatePath => false, File.ReadAllText);

        var action = loader.Invoking(candidate => candidate.Load());

        action.Should().Throw<InvalidDataException>().WithMessage("*Missing required configuration*");
    }

    private static TomlApplicationConfigurationLoader Loader(string contents) => new(
        "config.toml",
        candidatePath => true,
        candidatePath => contents);

    private static ConsoleKeyInfo Key(
        ConsoleKey key,
        bool shift = false,
        bool alt = false,
        bool control = false) => new('\0', key, shift, alt, control);

    private static ConsoleKeyInfo Key(char character) =>
        new(character, ConsoleKey.NoName, false, false, false);
}
