using System.Globalization;

namespace WolfTodo.Tui.Features.DayPlanner;

public static class DayScheduleMarkdownDocument
{
    public static string ReplaceDaySection(string contents, DateOnly date, string section)
    {
        var heading = Heading(date);
        var start = contents.IndexOf(heading, StringComparison.Ordinal);
        if (start < 0)
        {
            return contents.Length == 0
                ? section + Environment.NewLine
                : contents.TrimEnd() + Environment.NewLine + Environment.NewLine + section + Environment.NewLine;
        }

        var nextHeading = contents.IndexOf("# 📅 ", start + heading.Length, StringComparison.Ordinal);
        var end = nextHeading < 0 ? contents.Length : nextHeading;
        var before = contents[..start];
        var after = contents[end..].TrimStart('\r', '\n');
        var prefix = before.Length == 0
            ? string.Empty
            : before.EndsWith(Environment.NewLine + Environment.NewLine, StringComparison.Ordinal)
                ? before
                : before.EndsWith(Environment.NewLine, StringComparison.Ordinal)
                    ? before + Environment.NewLine
                    : before + Environment.NewLine + Environment.NewLine;
        return after.Length == 0
            ? prefix + section + Environment.NewLine
            : prefix + section + Environment.NewLine + Environment.NewLine + after;
    }

    public static string Heading(DateOnly date) =>
        $"# 📅 {date.ToString("dddd, dd MMM yyyy", CultureInfo.InvariantCulture)}";
}
