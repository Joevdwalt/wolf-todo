using System.Globalization;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public static class DayScheduleMarkdownPath
{
    public static string Create(DateOnly date, DayScheduleExportConfiguration configuration) =>
        Path.Combine(
            configuration.NotesDirectory,
            date.ToString("yyyy", CultureInfo.InvariantCulture),
            date.ToString("MM", CultureInfo.InvariantCulture),
            $"Week - {ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue))}.md");
}
