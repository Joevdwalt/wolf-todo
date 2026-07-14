using FluentAssertions;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class NaturalStringComparerTests
{
    [Fact]
    public void Compare_orders_numeric_chunks_by_value_case_insensitively()
    {
        var values = new[] { "Task 10", "task 2", "Task 1" };

        var result = values.OrderBy(value => value, NaturalStringComparer.Instance);

        result.Should().Equal("Task 1", "task 2", "Task 10");
    }

    [Fact]
    public void Compare_uses_leading_zero_count_as_a_stable_numeric_tie_breaker()
    {
        NaturalStringComparer.Instance.Compare("Task 2", "Task 02").Should().BeNegative();
    }
}
