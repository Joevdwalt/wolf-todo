using System.Globalization;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class DayScheduleMarkdownRenderer
{
    public string Render(PlannerView view, DayScheduleExportConfiguration configuration)
    {
        var lines = new List<string> { DayScheduleMarkdownDocument.Heading(view.State.SelectedDate) };
        lines.AddRange(configuration.ProjectLinks);
        lines.Add(string.Empty);
        lines.Add("## All day");
        lines.AddRange(view.CalendarAgenda.AllDayItems.Select(item => $"- {FormatTitle(item.Title, item.IsCompleted)}"));
        lines.Add(string.Empty);
        lines.Add("## Time blocks");

        for (var start = new TimeOnly(9, 0); start < new TimeOnly(17, 0); start = start.AddMinutes(30))
        {
            var end = start.AddMinutes(30);
            var titles = view.Slots
                .SelectMany(slot => slot.Items)
                .GroupBy(item => item.Identity, StringComparer.Ordinal)
                .Select(group => group.First())
                .Where(item => Occupies(item, start, end))
                .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Identity, StringComparer.Ordinal)
                .Select(item => FormatTitle(item.Title, item.IsCompleted));
            lines.Add($"**{start.ToString("HH:mm", CultureInfo.InvariantCulture)} - " +
                      $"{end.ToString("HH:mm", CultureInfo.InvariantCulture)}** - {string.Join(" · ", titles)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool Occupies(PlannerTimelineItemView item, TimeOnly start, TimeOnly end) =>
        item.TimeShape == PlannerTimeShape.Instant
            ? item.Start >= start && item.Start < end
            : item.Start < end && item.End > start;

    private static string FormatTitle(string title, bool isCompleted) =>
        isCompleted ? $"~~{title}~~" : title;
}
