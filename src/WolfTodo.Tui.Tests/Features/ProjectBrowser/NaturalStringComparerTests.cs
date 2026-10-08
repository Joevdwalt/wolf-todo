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

    [Theory]
    [InlineData("2", "10", -1)]
    [InlineData("12", "11", 1)]
    [InlineData("42", "42", 0)]
    [InlineData("2", "02", -1)]
    [InlineData("002", "02", 1)]
    [InlineData("000", "0", 1)]
    [InlineData("99999999999999999999", "100000000000000000000", -1)]
    public void CompareNumber_orders_digit_runs_and_consumes_both_runs(
        string left,
        string right,
        int expectedSign)
    {
        var leftIndex = 0;
        var rightIndex = 0;

        var result = NaturalStringComparer.CompareNumber(left, ref leftIndex, right, ref rightIndex);

        Math.Sign(result).Should().Be(expectedSign);
        leftIndex.Should().Be(left.Length);
        rightIndex.Should().Be(right.Length);
    }

    [Fact]
    public void CompareNumber_starts_at_supplied_indexes_and_stops_before_suffixes()
    {
        var leftIndex = 5;
        var rightIndex = 2;

        var result = NaturalStringComparer.CompareNumber(
            "Task 12a", ref leftIndex, "T 12b", ref rightIndex);

        result.Should().Be(0);
        leftIndex.Should().Be(7);
        rightIndex.Should().Be(4);
    }
}
