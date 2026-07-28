using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class DayScheduleMarkdownPathTests
{
    [Fact]
    public void Create_uses_selected_date_month_and_iso_week_file()
    {
        var result = DayScheduleMarkdownPath.Create(
            new DateOnly(2026, 7, 13),
            new DayScheduleExportConfiguration("/notes", []));

        result.Should().Be(Path.Combine("/notes", "2026", "07", "Week - 29.md"));
    }
}
