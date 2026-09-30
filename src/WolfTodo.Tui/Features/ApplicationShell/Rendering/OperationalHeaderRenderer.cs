using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.ApplicationShell.Rendering;

public sealed class OperationalHeaderRenderer
{
    private readonly SurfaceThemeRenderer themeRenderer = new();

    public void Write(
        TabStripView view,
        TuiKeyBindings bindings,
        TuiTheme theme,
        int terminalWidth,
        string mode,
        DateOnly date,
        DateTime now,
        int openCount,
        int errorCount,
        DateOnly? rangeStart = null,
        DateOnly? rangeEnd = null)
    {
        var segments = new List<(string Text, Color Color, Decoration Decoration)>();
        var hasSelectedRange = rangeStart is not null && rangeEnd is not null;
        if (terminalWidth >= 60 && (!hasSelectedRange || terminalWidth >= 100))
        {
            segments.Add(("WOLF TODO // ", theme.Heading, Decoration.Bold));
            for (var index = 0; index < view.Tabs.Length; index++)
            {
                if (index > 0)
                {
                    segments.Add(("  ", theme.Text, Decoration.None));
                }

                var tab = view.Tabs[index];
                var title = tab.IsSelected
                    ? $"[{tab.Title.ToUpperInvariant()}]"
                    : tab.Title.ToUpperInvariant();
                var color = tab.IsSelected ? theme.Accent : theme.Muted;
                var decoration = tab.IsSelected ? Decoration.Bold : Decoration.Dim;
                segments.Add((title, color, decoration));
            }
        }
        else
        {
            var active = view.Tabs.First(tab => tab.IsSelected);
            segments.Add(($"[{active.Title.ToUpperInvariant()}]", theme.Accent, Decoration.Bold));
        }

        if (hasSelectedRange)
        {
            segments.Add(($"  {DateRangePositionLabel(rangeStart!.Value, rangeEnd!.Value, date)}", theme.Date, Decoration.None));
        }

        segments.Add(($"  TIME:{now:HH:mm}", theme.Now, Decoration.Bold));
        segments.Add(($"  MODE:{mode}", theme.SecondaryText, Decoration.None));
        if (!hasSelectedRange && terminalWidth >= 80)
        {
            segments.Add(($"  {date.ToString("ddd dd MMM").ToUpperInvariant()}", theme.Date, Decoration.None));
        }

        if (terminalWidth >= 100)
        {
            segments.Add(($"  OPEN:{openCount}", theme.Text, Decoration.None));
            segments.Add((
                errorCount == 0 ? "  FILES:CLEAN" : $"  FILES:{errorCount} ERRORS",
                errorCount == 0 ? theme.Muted : theme.Error,
                errorCount == 0 ? Decoration.Dim : Decoration.Bold));
        }

        if (terminalWidth >= 120 && view.Tabs.Length > 1)
        {
            var hint = $"  {TuiKeyBindings.ShortestDisplayName(bindings.TabPrevious)}/" +
                       $"{TuiKeyBindings.ShortestDisplayName(bindings.TabNext)} TABS";
            segments.Add((hint, theme.Muted, Decoration.Dim));
        }

        var totalLength = segments.Sum(segment => segment.Text.Length);
        var width = Math.Max(1, terminalWidth);
        var remaining = totalLength > width ? width - 1 : width;
        var output = new StringBuilder();

        foreach (var segment in segments)
        {
            var length = Math.Min(segment.Text.Length, remaining);
            if (length == 0)
            {
                break;
            }

            themeRenderer.AppendStyled(output, segment.Text[..length], segment.Color, segment.Decoration);
            remaining -= length;
        }

        if (totalLength > width)
        {
            themeRenderer.AppendStyled(output, "…", theme.Muted);
        }

        themeRenderer.WriteSurface(new Markup(output.ToString()), theme.Background, true);
        AnsiConsole.WriteLine();
    }

    public static string DateRangeLabel(DateOnly start, DateOnly end) =>
        start == end
            ? start.ToString("ddd dd MMM").ToUpperInvariant()
            : start.Month == end.Month
            ? $"{start:ddd dd}–{end:ddd dd MMM}".ToUpperInvariant()
            : $"{start:ddd dd MMM}–{end:ddd dd MMM}".ToUpperInvariant();

    public static string DateRangePositionLabel(DateOnly start, DateOnly end, DateOnly selected)
    {
        var position = Math.Clamp(selected.DayNumber - start.DayNumber + 1, 1, end.DayNumber - start.DayNumber + 1);
        var count = end.DayNumber - start.DayNumber + 1;
        return $"{DateRangeLabel(start, end)} ({position} OF {count})";
    }
}
