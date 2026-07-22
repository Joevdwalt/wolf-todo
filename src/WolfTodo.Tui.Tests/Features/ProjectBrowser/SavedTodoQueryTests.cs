using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class SavedTodoQueryTests
{
    private static readonly DateOnly Today = new(2026, 7, 22);

    [Theory]
    [InlineData("scheduled:t-1", 2026, 7, 21, true)]
    [InlineData("scheduled:<t", 2026, 7, 21, true)]
    [InlineData("scheduled:<t", 2026, 7, 22, false)]
    [InlineData("scheduled:>=t", 2026, 7, 23, true)]
    public void Matches_supports_relative_scheduled_date_comparisons(
        string source,
        int year,
        int month,
        int day,
        bool expected)
    {
        SavedTodoQuery.TryParse(source, out var query, out _).Should().BeTrue();
        var todo = Todo("Task") with { Schedule = new TodoSchedule(new DateOnly(year, month, day)) };

        query.Matches(todo, "Work", Today).Should().Be(expected);
    }

    [Fact]
    public void Matches_combines_fields_with_and_semantics()
    {
        SavedTodoQuery.TryParse("scheduled:t-1 tag:work project:client priority:high", out var query, out _)
            .Should().BeTrue();
        var todo = Todo("Task") with
        {
            Schedule = new TodoSchedule(Today.AddDays(-1)),
            Tags = ["work"],
            Priority = TodoPriority.High
        };

        query.Matches(todo, "Client Alpha", Today).Should().BeTrue();
        query.Matches(todo, "Personal", Today).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("yesterday")]
    [InlineData("due:t-1")]
    [InlineData("scheduled:tomorrow")]
    [InlineData("priority:urgent")]
    public void TryParse_rejects_invalid_queries(string source)
    {
        SavedTodoQuery.TryParse(source, out _, out var error).Should().BeFalse();
        error.Should().NotBeNullOrWhiteSpace();
    }

    private static TodoItem Todo(string title) =>
        new(1, false, null, title, null, [], null, null, string.Empty, [], []);
}
