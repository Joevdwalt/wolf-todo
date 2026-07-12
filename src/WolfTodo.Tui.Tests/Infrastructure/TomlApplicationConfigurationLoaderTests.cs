using FluentAssertions;
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
}
