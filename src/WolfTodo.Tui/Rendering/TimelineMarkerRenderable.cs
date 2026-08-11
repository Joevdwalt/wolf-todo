using Spectre.Console;
using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Rendering;

internal sealed class TimelineMarkerRenderable(
    Style style,
    TimeSpan? timeUntilNextMeeting = null,
    string? nextMeetingTitle = null) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) =>
        new(maxWidth, maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            yield break;
        }

        var label = Label(maxWidth);
        var labelWidth = label.GetCellWidth();
        var text = labelWidth >= maxWidth
            ? Truncate(label, maxWidth)
            : label + new string('━', maxWidth - labelWidth);
        yield return new Segment(text, style, null);
    }

    private string Label(int maxWidth)
    {
        if (timeUntilNextMeeting is not { } countdown)
        {
            return "┣━━ NOW ";
        }

        var prefix = $"┣━━ NOW · {FormatDuration(countdown)}";
        var title = Normalize(nextMeetingTitle);
        if (title is null)
        {
            return prefix + " ";
        }

        const int minimumLineWidth = 3;
        var titleWidth = maxWidth - prefix.GetCellWidth() - " · ".GetCellWidth() - minimumLineWidth - 1;
        return titleWidth <= 0
            ? prefix + " "
            : $"{prefix} · {Ellipsize(title, titleWidth)} ";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = (int)duration.TotalMinutes;
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        if (hours == 0)
        {
            return $"{minutes}m";
        }

        return minutes == 0 ? $"{hours}h" : $"{hours}h {minutes:00}m";
    }

    private static string? Normalize(string? title)
    {
        var words = title?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return words is { Length: > 0 } ? string.Join(' ', words) : null;
    }

    private static string Ellipsize(string value, int maxWidth)
    {
        if (value.GetCellWidth() <= maxWidth)
        {
            return value;
        }

        return maxWidth == 1 ? "…" : Truncate(value, maxWidth - 1) + "…";
    }

    private static string Truncate(string value, int maxWidth)
    {
        var result = new System.Text.StringBuilder();
        var width = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var runeText = rune.ToString();
            var runeWidth = runeText.GetCellWidth();
            if (width + runeWidth > maxWidth)
            {
                break;
            }

            result.Append(runeText);
            width += runeWidth;
        }

        return result.ToString();
    }
}
