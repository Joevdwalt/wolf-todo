using FluentAssertions;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class DateExpressionTests
{
    private static readonly DateOnly Today = new(2026, 7, 20);

    [Theory]
    [InlineData("2026-08-01", 2026, 8, 1)]
    [InlineData("t", 2026, 7, 20)]
    [InlineData("t+1", 2026, 7, 21)]
    [InlineData("t-3", 2026, 7, 17)]
    [InlineData("w+1", 2026, 7, 27)]
    [InlineData("w-2", 2026, 7, 6)]
    public void TryParse_accepts_iso_and_relative_date_expressions(
        string expression,
        int year,
        int month,
        int day)
    {
        var parsed = DateExpression.TryParse(expression, Today, out var date);

        parsed.Should().BeTrue();
        date.Should().Be(new DateOnly(year, month, day));
    }

    [Theory]
    [InlineData("today")]
    [InlineData("w")]
    [InlineData("t+")]
    [InlineData("w+one")]
    [InlineData("2026-02-29")]
    public void TryParse_rejects_invalid_expressions(string expression)
    {
        DateExpression.TryParse(expression, Today, out _).Should().BeFalse();
    }
}
