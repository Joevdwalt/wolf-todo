using FluentAssertions;

using WolfTodo.Cli;

namespace WolfTodo.Cli.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    public void Run_returns_success()
    {
        new CliApplication().Run().Should().Be(0);
    }
}
