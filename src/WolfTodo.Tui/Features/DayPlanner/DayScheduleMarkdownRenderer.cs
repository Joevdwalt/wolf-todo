using System.Globalization;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class DayScheduleMarkdownRenderer
{
    private static readonly TimeOnly DefaultStart = new(9, 0);
    private static readonly TimeOnly DefaultEnd = new(17, 0);

    public string Render(PlannerView view, DayScheduleExportConfiguration configuration)
    {
        var exportPath = DayScheduleMarkdownPath.Create(view.State.SelectedDate, configuration);
        var exportDirectory = Path.GetDirectoryName(exportPath) ?? configuration.NotesDirectory;
        var timedItems = view.Slots
            .SelectMany(slot => slot.Items)
            .GroupBy(item => item.Identity, StringComparer.Ordinal)
            .Select(group => group.First())
            .Where(item => item.ItemType != PlannerItemType.Pomodoro)
            .ToArray();
        var range = ExportRange(timedItems);

        var lines = new List<string> { DayScheduleMarkdownDocument.Heading(view.State.SelectedDate) };
        lines.AddRange(configuration.ProjectLinks);
        lines.Add(string.Empty);
        lines.Add("## All day");
        lines.AddRange(view.CalendarAgenda.AllDayItems.Select(item =>
            $"- {FormatTitle(item.Title, item.IsCompleted, item.Assignment, exportDirectory)}"));
        lines.Add(string.Empty);
        lines.Add("## Time blocks");

        for (var start = range.Start; start < range.End; start = start.AddMinutes(30))
        {
            var end = start.AddMinutes(30);
            var titles = timedItems
                .Where(item => Occupies(item, start, end))
                .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Identity, StringComparer.Ordinal)
                .Select(item => FormatTitle(item.Title, item.IsCompleted, item.Assignment, exportDirectory));
            lines.Add($"**{start.ToString("HH:mm", CultureInfo.InvariantCulture)} - " +
                      $"{end.ToString("HH:mm", CultureInfo.InvariantCulture)}** - {string.Join(" · ", titles)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool Occupies(PlannerTimelineItemView item, TimeOnly start, TimeOnly end) =>
        item.TimeShape == PlannerTimeShape.Instant
            ? item.Start >= start && item.Start < end
            : item.Start < end && item.End > start;

    private static (TimeOnly Start, TimeOnly End) ExportRange(PlannerTimelineItemView[] items)
    {
        if (items.Length == 0)
        {
            return (DefaultStart, DefaultEnd);
        }

        var start = items.Min(item => item.Start);
        var finalItemEnd = items.Max(item => item.TimeShape == PlannerTimeShape.Instant
            ? item.Start.AddMinutes(30)
            : item.End);
        return (start, finalItemEnd > DefaultEnd ? finalItemEnd : DefaultEnd);
    }

    private static string FormatTitle(
        string title,
        bool isCompleted,
        PlannerAssignment? assignment,
        string exportDirectory)
    {
        var text = assignment is null
            ? title
            : $"[{EscapeLinkLabel(title)}]({RelativeMarkdownPath(exportDirectory, assignment.ProjectPath)})";
        return isCompleted ? $"~~{text}~~" : text;
    }

    private static string EscapeLinkLabel(string title) => title
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal)
        .Replace("]", "\\]", StringComparison.Ordinal);

    private static string RelativeMarkdownPath(string exportDirectory, string projectPath) => string.Join(
        '/',
        Path.GetRelativePath(exportDirectory, projectPath)
            .Replace('\\', '/')
            .Split('/')
            .Select(Uri.EscapeDataString));
}
