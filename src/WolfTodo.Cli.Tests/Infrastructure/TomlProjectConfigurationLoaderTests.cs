using FluentAssertions;
using WolfTodo.Cli.Infrastructure;

namespace WolfTodo.Cli.Tests.Infrastructure;

public sealed class TomlProjectConfigurationLoaderTests
{
    [Fact]
    public void Load_reads_project_files_and_ignores_host_specific_tables()
    {
        var loader = new TomlProjectConfigurationLoader(
            "/config.toml",
            path => true,
            path => "[projects]\nfiles = [\"/todos/work.md\"]\n[tui.theme]\npreset = \"wolf\"\n");

        loader.Load().Should().Equal("/todos/work.md");
    }

    [Fact]
    public void Load_rejects_a_missing_configuration()
    {
        var loader = new TomlProjectConfigurationLoader("/missing.toml", path => false, path => string.Empty);

        var action = loader.Load;

        action.Should().Throw<InvalidDataException>().WithMessage("Missing required configuration file*");
    }
}
