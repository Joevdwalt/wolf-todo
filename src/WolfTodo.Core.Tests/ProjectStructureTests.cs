using FluentAssertions;

using WolfTodo.Core;

namespace WolfTodo.Core.Tests;

public sealed class CoreAssemblyTests
{
    [Fact]
    public void Name_identifies_the_shared_core_project()
    {
        CoreAssembly.Name.Should().Be("WolfTodo.Core");
    }
}
