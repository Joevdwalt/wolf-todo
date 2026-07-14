using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.Configuration;

public sealed class GlobalApplicationStatePathTests
{
    [Fact]
    public void Resolve_returns_the_platform_state_file()
    {
        var result = GlobalApplicationStatePath.Resolve();

        result.Should().EndWith(Path.Combine("wtodo", "state.json"));
        Path.IsPathFullyQualified(result).Should().BeTrue();
    }
}
