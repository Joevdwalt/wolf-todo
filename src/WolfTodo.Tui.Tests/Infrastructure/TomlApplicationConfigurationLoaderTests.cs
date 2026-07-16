using FluentAssertions;
using Spectre.Console;
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
        result.KeyBindings.HelpCommand.Should().Be(":help");
        result.KeyBindings.MatchesCommandPalette(Key('?')).Should().BeTrue();
        result.KeyBindings.MatchesEditTodoContent(Key('E')).Should().BeTrue();
        result.KeyBindings.MatchesEditTodoExternal(Key(ConsoleKey.E, control: true)).Should().BeTrue();
        result.KeyBindings.MatchesToggleDetails(Key('v')).Should().BeTrue();
        result.KeyBindings.MatchesMoveDown(Key('j')).Should().BeTrue();
        result.KeyBindings.MatchesMoveUp(Key(ConsoleKey.UpArrow)).Should().BeTrue();
        result.KeyBindings.MatchesJumpTop(Key('g')).Should().BeTrue();
        result.KeyBindings.MatchesJumpBottom(Key('G')).Should().BeTrue();
        result.KeyBindings.MatchesSortMode(Key('t')).Should().BeTrue();
        result.KeyBindings.MatchesTabNext(Key('L')).Should().BeTrue();
        result.KeyBindings.MatchesTabPrevious(Key('H')).Should().BeTrue();
        result.Theme.Should().Be(TuiThemes.Wolf);
    }

    [Fact]
    public void Load_resolves_a_theme_preset_and_individual_color_overrides()
    {
        var path = Path.GetFullPath("todo.md");
        var loader = Loader($$"""
            [projects]
            files = ["{{path}}"]

            [keybindings]
            quit = ":q"

            [tui.theme]
            preset = "CLASSIC"
            accent = "#123456"
            tag = "aquamarine3"
            text = "default"
            """);

        var result = loader.Load();

        result.Theme.Accent.Should().Be(new Color(0x12, 0x34, 0x56));
        result.Theme.Tag.Should().Be(Color.Aquamarine3);
        result.Theme.Text.Should().Be(Color.Default);
        result.Theme.Error.Should().Be(Color.Red);
    }

    [Theory]
    [InlineData("preset = \"unknown\"", "*tui.theme.preset*wolf*classic*mono*")]
    [InlineData("accent = \"#12345\"", "*tui.theme.accent*named color*#RRGGBB*")]
    [InlineData("accent = \"not-a-color\"", "*tui.theme.accent*named color*#RRGGBB*")]
    [InlineData("accent = 42", "*tui.theme.accent*named color*#RRGGBB*")]
    [InlineData("unknown = \"red\"", "*tui.theme.unknown*not a supported*")]
    public void Load_rejects_invalid_theme_configuration(string setting, string expectedMessage)
    {
        var path = Path.GetFullPath("todo.md");
        var loader = Loader($$"""
            [projects]
            files = ["{{path}}"]

            [keybindings]
            quit = ":q"

            [tui.theme]
            {{setting}}
            """);

        var action = loader.Invoking(candidate => candidate.Load());

        action.Should().Throw<InvalidDataException>().WithMessage(expectedMessage);
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
            help = ":commands"
            move_down = ["n", "Ctrl+J"]
            jump_top = ["Home"]
            jump_bottom = ["End"]
            sort_mode = ["Ctrl+S"]
            tab_next = ["Alt+RightArrow"]
            command_palette = ["Ctrl+P"]
            edit_todo_content = ["Ctrl+E"]
            edit_todo_external = ["Ctrl+X"]
            toggle_details = ["Ctrl+V"]
            """);

        var result = loader.Load();

        result.KeyBindings.ToggleCompletedCommand.Should().Be(":done");
        result.KeyBindings.HelpCommand.Should().Be(":commands");
        result.KeyBindings.MatchesCommandPalette(Key(ConsoleKey.P, control: true)).Should().BeTrue();
        result.KeyBindings.MatchesEditTodoContent(Key(ConsoleKey.E, control: true)).Should().BeTrue();
        result.KeyBindings.MatchesEditTodoExternal(Key(ConsoleKey.X, control: true)).Should().BeTrue();
        result.KeyBindings.MatchesToggleDetails(Key(ConsoleKey.V, control: true)).Should().BeTrue();
        result.KeyBindings.MatchesMoveDown(Key('n')).Should().BeTrue();
        result.KeyBindings.MatchesMoveDown(Key('j')).Should().BeFalse();
        result.KeyBindings.MatchesMoveDown(Key(ConsoleKey.J, control: true)).Should().BeTrue();
        result.KeyBindings.MatchesMoveUp(Key('k')).Should().BeTrue();
        result.KeyBindings.MatchesJumpTop(Key(ConsoleKey.Home)).Should().BeTrue();
        result.KeyBindings.MatchesJumpBottom(Key(ConsoleKey.End)).Should().BeTrue();
        result.KeyBindings.MatchesSortMode(Key(ConsoleKey.S, control: true)).Should().BeTrue();
        result.KeyBindings.MatchesSortMode(Key('t')).Should().BeFalse();
        result.KeyBindings.MatchesTabNext(Key(ConsoleKey.RightArrow, alt: true)).Should().BeTrue();
        result.KeyBindings.MatchesTabNext(Key(ConsoleKey.Tab, control: true)).Should().BeFalse();
    }

    [Theory]
    [InlineData("move_up = []", "*move_up*non-empty*")]
    [InlineData("move_up = [\"Meta+K\"]", "*move_up*Invalid modifier*")]
    [InlineData("move_up = [\"k\", \"k\"]", "*move_up*duplicate*")]
    [InlineData("move_up = [\"j\"]", "*both*move_up*move_down*")]
    [InlineData("tab_next = [\"Tab\"]", "*both*focus_next*tab_next*")]
    [InlineData("sort_mode = [\"j\"]", "*both*move_down*sort_mode*")]
    [InlineData("jump_top = [\"j\"]", "*both*move_down*jump_top*")]
    [InlineData("edit_todo_external = [\"e\"]", "*both*edit_todo*edit_todo_external*")]
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
