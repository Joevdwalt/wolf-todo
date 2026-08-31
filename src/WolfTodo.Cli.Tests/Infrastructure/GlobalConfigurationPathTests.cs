using FluentAssertions;
using WolfTodo.Cli.Infrastructure;

namespace WolfTodo.Cli.Tests.Infrastructure;

public sealed class GlobalConfigurationPathTests
{
    [Fact]
    public void Resolve_uses_the_platform_wtodo_configuration_location()
    {
        var result = GlobalConfigurationPath.Resolve();

        result.Should().EndWith(Path.Combine("wtodo", "config.toml"));
        Path.IsPathFullyQualified(result).Should().BeTrue();
    }
}
