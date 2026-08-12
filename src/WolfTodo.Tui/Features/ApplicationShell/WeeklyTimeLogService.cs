using System.Globalization;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ApplicationShell;

public interface IWeeklyTimeLogFileStore
{
    bool FileExists(string path);
    string ReadAllText(string path);
    void WriteAllTextAtomically(string path, string contents);
}

public sealed record TimeLogResult(bool Succeeded, string? Error = null)
{
    public static TimeLogResult Success() => new(true);
    public static TimeLogResult Failure(string error) => new(false, error);
}

public sealed class WeeklyTimeLogService(IWeeklyTimeLogFileStore fileStore)
{
    public TimeLogResult Record(ActiveTimer timer, DateTime endedAt, TimerConfiguration? configuration)
    {
        if (configuration is null)
        {
            return TimeLogResult.Failure("Task timing requires a [timer] notes_directory configuration.");
        }

        try
        {
            for (var start = timer.StartedAt; start < endedAt;)
            {
                var nextMidnight = start.Date.AddDays(1);
                var end = endedAt < nextMidnight ? endedAt : nextMidnight;
                WriteSegment(timer, start, end, configuration);
                start = end;
            }

            return TimeLogResult.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return TimeLogResult.Failure($"Could not write task time: {exception.Message}");
        }
    }

    private void WriteSegment(ActiveTimer timer, DateTime start, DateTime end, TimerConfiguration configuration)
    {
        var path = PathFor(start, configuration);
        var contents = fileStore.FileExists(path) ? fileStore.ReadAllText(path) : string.Empty;
        var heading = $"## {start:dddd, dd MMM yyyy}";
        var header = $"# Time log · Week {ISOWeek.GetWeekOfYear(start)}";
        var entry = $"- {start:HH:mm}–{end:HH:mm} · {Minutes(start, end)}m — {timer.ProjectTitle} · {timer.TodoTitle}";
        if (string.IsNullOrWhiteSpace(contents))
        {
            contents = $"{header}\n\n{heading}\n\n{entry}\n";
        }
        else if (!contents.Contains(heading, StringComparison.Ordinal))
        {
            contents = contents.TrimEnd() + $"\n\n{heading}\n\n{entry}\n";
        }
        else
        {
            contents = contents.TrimEnd() + $"\n{entry}\n";
        }

        fileStore.WriteAllTextAtomically(path, contents);
    }

    private static int Minutes(DateTime start, DateTime end) => Math.Max(1, (int)Math.Ceiling((end - start).TotalMinutes));

    private static string PathFor(DateTime date, TimerConfiguration configuration) => Path.Combine(
        configuration.NotesDirectory,
        date.ToString("yyyy", CultureInfo.InvariantCulture),
        date.ToString("MM", CultureInfo.InvariantCulture),
        $"Time - {ISOWeek.GetWeekOfYear(date)}.md");
}
