using Spectre.Console;
using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Rendering;

internal sealed class TimelineMarkerRenderable(
    Style nowStyle,
    TimeSpan? timeUntilNextMeeting = null,
    string? nextMeetingTitle = null,
    Style? timerStyle = null,
    TimeSpan? pomodoroRemaining = null,
    string? pomodoroTitle = null) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) => new(maxWidth, maxWidth);

    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            yield break;
        }

        var segments = pomodoroRemaining is { } remaining
            ? PomodoroSegments(maxWidth, remaining)
            : MeetingSegments(maxWidth);
        var usedWidth = segments.Sum(segment => segment.Text.GetCellWidth());
        if (usedWidth < maxWidth)
        {
            segments.Add(new Segment(new string('━', maxWidth - usedWidth), nowStyle, null));
        }

        foreach (var segment in segments)
        {
            yield return segment;
        }
    }

    private List<Segment> MeetingSegments(int maxWidth)
    {
        var label = MeetingLabel(maxWidth);
        return [new Segment(Truncate(label, maxWidth), nowStyle, null)];
    }

    private List<Segment> PomodoroSegments(int maxWidth, TimeSpan remaining)
    {
        var timer = timerStyle ?? nowStyle;
        const string nowPrefix = "┣━━ NOW · ";
        var pomodoroCountdown = $"◷ {FormatCountdown(remaining)}";
        var meetingPrefix = timeUntilNextMeeting is { } meetingCountdown
            ? $" · NEXT {FormatDuration(meetingCountdown)}"
            : string.Empty;
        const string trailingSpace = " ";

        var fixedWidth = nowPrefix.GetCellWidth() + pomodoroCountdown.GetCellWidth() +
                         meetingPrefix.GetCellWidth() + trailingSpace.GetCellWidth();
        if (fixedWidth > maxWidth)
        {
            return TruncatedFixedSegments(maxWidth, nowPrefix, pomodoroCountdown, timer);
        }

        var taskTitle = Normalize(pomodoroTitle);
        var meetingTitle = Normalize(nextMeetingTitle);
        var available = maxWidth - fixedWidth;
        var taskPart = TitlePart(taskTitle, available);
        available -= taskPart.GetCellWidth();
        var meetingPart = TitlePart(meetingTitle, available);

        return
        [
            new Segment(nowPrefix, nowStyle, null),
            new Segment(pomodoroCountdown + taskPart, timer, null),
            new Segment(meetingPrefix + meetingPart + trailingSpace, nowStyle, null)
        ];
    }

    private List<Segment> TruncatedFixedSegments(
        int maxWidth,
        string nowPrefix,
        string pomodoroCountdown,
        Style timer)
    {
        var nowText = Truncate(nowPrefix, maxWidth);
        var remainingWidth = maxWidth - nowText.GetCellWidth();
        var timerText = remainingWidth > 0 ? Truncate(pomodoroCountdown, remainingWidth) : string.Empty;
        return
        [
            new Segment(nowText, nowStyle, null),
            new Segment(timerText, timer, null)
        ];
    }

    private string MeetingLabel(int maxWidth)
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
        return titleWidth <= 0 ? prefix + " " : $"{prefix} · {Ellipsize(title, titleWidth)} ";
    }

    private static string TitlePart(string? title, int availableWidth)
    {
        if (title is null || availableWidth <= " · ".GetCellWidth())
        {
            return string.Empty;
        }

        var titleWidth = availableWidth - " · ".GetCellWidth();
        return $" · {Ellipsize(title, titleWidth)}";
    }

    private static string FormatCountdown(TimeSpan duration)
    {
        var totalSeconds = Math.Max(0, (int)Math.Ceiling(duration.TotalSeconds));
        return totalSeconds >= 3600
            ? $"{totalSeconds / 3600:00}:{totalSeconds % 3600 / 60:00}:{totalSeconds % 60:00}"
            : $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = Math.Max(0, (int)duration.TotalMinutes);
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return hours == 0 ? $"{minutes}m" : minutes == 0 ? $"{hours}h" : $"{hours}h {minutes:00}m";
    }

    private static string? Normalize(string? title)
    {
        var words = title?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return words is { Length: > 0 } ? string.Join(' ', words) : null;
    }

    private static string Ellipsize(string value, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

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
