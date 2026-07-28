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
    [InlineData("mon", 2026, 7, 27)]
    [InlineData("monday", 2026, 7, 27)]
    [InlineData("tue", 2026, 7, 21)]
    [InlineData("tuesday", 2026, 7, 21)]
    [InlineData("wed", 2026, 7, 22)]
    [InlineData("wednesday", 2026, 7, 22)]
    [InlineData("thu", 2026, 7, 23)]
    [InlineData("thursday", 2026, 7, 23)]
    [InlineData("fri", 2026, 7, 24)]
    [InlineData("friday", 2026, 7, 24)]
    [InlineData("sat", 2026, 7, 25)]
    [InlineData("saturday", 2026, 7, 25)]
    [InlineData("sun", 2026, 7, 26)]
    [InlineData("sunday", 2026, 7, 26)]
    [InlineData(" MONDAY ", 2026, 7, 27)]
    public void TryParse_resolves_short_and_full_weekday_names_to_the_next_occurrence(
        string expression,
        int year,
        int month,
        int day)
    {
        DateExpression.TryParse(expression, Today, out var date).Should().BeTrue();

        date.Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    public void TryParse_moves_a_matching_weekday_to_the_following_week()
    {
        DateExpression.TryParse("mon", Today, out var date).Should().BeTrue();

        date.Should().Be(new DateOnly(2026, 7, 27));
    }

    [Fact]
    public void TryParse_rolls_the_next_weekday_into_the_following_month()
    {
        var tuesday = new DateOnly(2026, 7, 28);

        DateExpression.TryParse("mon", tuesday, out var date).Should().BeTrue();

        date.Should().Be(new DateOnly(2026, 8, 3));
    }

    [Theory]
    [InlineData("today")]
    [InlineData("w")]
    [InlineData("t+")]
    [InlineData("w+one")]
    [InlineData("mo")]
    [InlineData("mondays")]
    [InlineData("2026-02-29")]
    public void TryParse_rejects_invalid_expressions(string expression)
    {
        DateExpression.TryParse(expression, Today, out _).Should().BeFalse();
    }
}
