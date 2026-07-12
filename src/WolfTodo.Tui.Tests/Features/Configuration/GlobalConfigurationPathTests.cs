using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.Configuration;

public sealed class GlobalConfigurationPathTests
{
    [Fact]
    public void Resolve_returns_the_platform_configuration_file()
    {
        var result = GlobalConfigurationPath.Resolve();

        result.Should().EndWith(Path.Combine("wtodo", "config.toml"));
        Path.IsPathFullyQualified(result).Should().BeTrue();
    }
}
