using FluentAssertions;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class DayScheduleExportResultTests
{
    [Fact]
    public void Success_sets_path_without_an_error()
    {
        var result = DayScheduleExportResult.Success("/notes/Week - 29.md");

        result.Should().BeEquivalentTo(new DayScheduleExportResult(true, "/notes/Week - 29.md", null));
    }

    [Fact]
    public void Failure_sets_error_without_a_path()
    {
        var result = DayScheduleExportResult.Failure("No access");

        result.Should().BeEquivalentTo(new DayScheduleExportResult(false, null, "No access"));
    }
}
