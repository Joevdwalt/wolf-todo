using FluentAssertions;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class TomlKeybindingsLoaderTests
{
    [Fact]
    public void Load_returns_the_configured_quit_command()
    {
        var loader = new TomlKeybindingsLoader("keybindings.toml", contentsPath => "[keybindings]\nquit = ':quit'");

        var result = loader.Load();

        result.QuitCommand.Should().Be(":quit");
    }

    [Fact]
    public void Load_throws_when_the_quit_command_is_missing()
    {
        var loader = new TomlKeybindingsLoader("keybindings.toml", contentsPath => "[keybindings]");

        var action = loader.Invoking(candidate => candidate.Load());

        action.Should().Throw<InvalidDataException>().WithMessage("*keybindings.quit*");
    }
}
