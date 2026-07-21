using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed partial class ProjectMarkdownParser
{
    private static readonly IReadOnlyDictionary<string, TodoPriority> Priorities =
        new Dictionary<string, TodoPriority>
        {
            ["🔺"] = TodoPriority.Highest,
            ["⏫"] = TodoPriority.High,
            ["🔼"] = TodoPriority.Medium,
            ["🔽"] = TodoPriority.Low,
            ["⏬"] = TodoPriority.Lowest
        };

    private readonly IDeserializer yamlDeserializer = new DeserializerBuilder().Build();

    public ProjectParseResult Parse(string path, string contents)
    {
        var lines = contents.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var titleResult = ParseTitle(path, lines);

        if (titleResult.Error is not null)
        {
            return ProjectParseResult.Failure(titleResult.Error);
        }

        var headings = new string?[6];
        var rootTodos = new List<TodoBuilder>();
        var taskStack = new Stack<TodoBuilder>();

        for (var index = titleResult.ContentStart; index < lines.Length; index++)
        {
            var line = lines[index];
            var heading = HeadingPattern().Match(line);

            if (heading.Success)
            {
                var level = heading.Groups[1].Value.Length;
                headings[level - 1] = heading.Groups[2].Value.Trim();

                for (var deeperLevel = level; deeperLevel < headings.Length; deeperLevel++)
                {
                    headings[deeperLevel] = null;
                }

                taskStack.Clear();
                continue;
            }

            var task = TaskPattern().Match(line);

            if (task.Success)
            {
                var indent = IndentWidth(task.Groups[1].Value);
                var parsed = ParseTodoLine(index + 1, task.Groups[2].Value, task.Groups[3].Value, headings);

                if (parsed.Error is not null)
                {
                    return ProjectParseResult.Failure($"{path}:{index + 1}: {parsed.Error}");
                }

                var builder = parsed.Builder!;

                while (taskStack.Count > 0 && taskStack.Peek().Indent >= indent)
                {
                    taskStack.Pop();
                }

                if (taskStack.Count > 0)
                {
                    taskStack.Peek().Subtasks.Add(builder);
                }
                else
                {
                    rootTodos.Add(builder);
                }

                builder.Indent = indent;
                taskStack.Push(builder);
                continue;
            }

            if (taskStack.Count > 0)
            {
                var indent = IndentWidth(line[..(line.Length - line.TrimStart().Length)]);
                var current = taskStack.Peek();
                var trimmed = line.TrimStart();
                var isContinuation = current.Notes.Count > 0 &&
                                     (string.IsNullOrWhiteSpace(line) || indent > current.Indent) &&
                                     !trimmed.StartsWith("- ", StringComparison.Ordinal) &&
                                     !trimmed.StartsWith("* ", StringComparison.Ordinal) &&
                                     !trimmed.StartsWith("+ ", StringComparison.Ordinal);
                if (isContinuation)
                {
                    var note = current.Notes[^1];
                    current.Notes[^1] = note with
                    {
                        Text = note.Text + "\n" + (string.IsNullOrWhiteSpace(line) ? string.Empty : trimmed),
                        LineCount = note.LineCount + 1
                    };
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                while (taskStack.Count > 0 && taskStack.Peek().Indent >= indent)
                {
                    taskStack.Pop();
                }

                if (taskStack.Count > 0)
                {
                    taskStack.Peek().Notes.Add(new TodoNote(index + 1, NormalizeNote(line.Trim())));
                }
            }
        }

        return ProjectParseResult.Success(new TodoProject(
            titleResult.Title!,
            path,
            [.. rootTodos.Select(todo => todo.Build())]));
    }

    private TitleResult ParseTitle(string path, string[] lines)
    {
        if (lines.Length == 0 || lines[0].Trim() != "---")
        {
            return new TitleResult(System.IO.Path.GetFileNameWithoutExtension(path), 0, null);
        }

        var closingLine = Array.FindIndex(lines, 1, line => line.Trim() == "---");

        if (closingLine < 0)
        {
            return new TitleResult(null, 0, $"{path}:1: YAML front matter is not closed.");
        }

        try
        {
            var yaml = string.Join('\n', lines[1..closingLine]);
            var document = yamlDeserializer.Deserialize<Dictionary<string, object?>>(yaml) ?? [];

            if (!document.TryGetValue("title", out var titleValue))
            {
                return new TitleResult(System.IO.Path.GetFileNameWithoutExtension(path), closingLine + 1, null);
            }

            if (titleValue is not string title || string.IsNullOrWhiteSpace(title))
            {
                return new TitleResult(null, 0, $"{path}:1: YAML title must be a non-empty string.");
            }

            return new TitleResult(title.Trim(), closingLine + 1, null);
        }
        catch (Exception exception)
        {
            return new TitleResult(null, 0, $"{path}:1: Invalid YAML front matter: {exception.Message}");
        }
    }

    private static TodoLineResult ParseTodoLine(int sourceLine, string status, string text, string?[] headings)
    {
        var externalReference = default(string);
        var referenceMatch = ReferencePattern().Match(text);

        if (referenceMatch.Success)
        {
            externalReference = referenceMatch.Groups[1].Value;
            text = text[referenceMatch.Length..];
        }

        var priorityMatches = Priorities
            .SelectMany(priority => Enumerable.Repeat(priority, CountOccurrences(text, priority.Key)))
            .ToArray();

        if (priorityMatches.Length > 1)
        {
            return new TodoLineResult(null, "Todo contains more than one priority marker.");
        }

        var startResult = ParseDate(text, StartDatePattern(), "start");

        if (startResult.Error is not null)
        {
            return new TodoLineResult(null, startResult.Error);
        }

        var dueResult = ParseDate(text, DueDatePattern(), "due");

        if (dueResult.Error is not null)
        {
            return new TodoLineResult(null, dueResult.Error);
        }

        var scheduleResult = ParseSchedule(text);

        if (scheduleResult.Error is not null)
        {
            return new TodoLineResult(null, scheduleResult.Error);
        }

        var durationResult = ParseDuration(text);
        if (durationResult.Error is not null)
        {
            return new TodoLineResult(null, durationResult.Error);
        }

        var tags = TagPattern().Matches(text)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        text = TagPattern().Replace(text, string.Empty);
        text = RemoveRecognizedToken(text, startResult.Token);
        text = RemoveRecognizedToken(text, dueResult.Token);
        text = RemoveRecognizedToken(text, scheduleResult.TimeToken);
        text = RemoveRecognizedToken(text, scheduleResult.DateToken);
        text = RemoveRecognizedToken(text, durationResult.Token);

        foreach (var marker in Priorities.Keys)
        {
            text = text.Replace(marker, string.Empty, StringComparison.Ordinal);
        }

        var title = WhitespacePattern().Replace(text, " ").Trim();

        if (title.Length == 0)
        {
            return new TodoLineResult(null, "Todo title must not be empty.");
        }

        var sectionPath = string.Join(" / ", headings.Where(heading => !string.IsNullOrWhiteSpace(heading)));
        TodoPriority? priority = priorityMatches.Length == 1 ? priorityMatches[0].Value : null;
        var builder = new TodoBuilder(
            sourceLine,
            !string.Equals(status, " ", StringComparison.Ordinal),
            externalReference,
            title,
            priority,
            tags,
            startResult.Date,
            dueResult.Date,
            sectionPath,
            scheduleResult.Schedule,
            durationResult.Duration);

        return new TodoLineResult(builder, null);
    }

    private static DateResult ParseDate(string text, Regex pattern, string fieldName)
    {
        var matches = pattern.Matches(text);

        if (matches.Count > 1)
        {
            return new DateResult(null, null, $"Todo contains more than one {fieldName} date.");
        }

        if (matches.Count == 0)
        {
            return new DateResult(null, null, null);
        }

        var value = matches[0].Groups[1].Value;

        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? new DateResult(date, matches[0].Value, null)
            : new DateResult(null, null, null);
    }

    private static ScheduleResult ParseSchedule(string text)
    {
        var dateMatches = ScheduleDatePattern().Matches(text);
        var timeMatches = ScheduleTimePattern().Matches(text);
        if (dateMatches.Count == 0)
        {
            return new ScheduleResult(null, null, null, null);
        }

        if (dateMatches.Count > 1 || timeMatches.Count > 1)
        {
            return new ScheduleResult(null, null, null, "Todo contains more than one schedule.");
        }

        var dateMatch = dateMatches[0];
        var timeMatch = timeMatches.Count == 0 ? null : timeMatches[0];
        if (!DateOnly.TryParseExact(
                dateMatch.Groups[1].Value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return new ScheduleResult(
                null,
                null,
                null,
                "Todo schedule must use a valid date and a quarter-hour time from 06:00 through 21:45.");
        }

        if (timeMatch is null)
        {
            return new ScheduleResult(new TodoSchedule(date), null, dateMatch.Value, null);
        }

        if (!TimeOnly.TryParseExact(
                timeMatch.Groups[1].Value,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time) ||
            time.Minute is not (0 or 15 or 30 or 45) ||
            time < new TimeOnly(6, 0) ||
            time >= new TimeOnly(22, 0))
        {
            return new ScheduleResult(
                null,
                null,
                null,
                "Todo schedule must use a valid date and a quarter-hour time from 06:00 through 21:45.");
        }

        return new ScheduleResult(
            new TodoSchedule(date, time),
            timeMatch.Value,
            dateMatch.Value,
            null);
    }

    private static DurationResult ParseDuration(string text)
    {
        var matches = DurationPattern().Matches(text);
        if (matches.Count > 1)
        {
            return new DurationResult(null, null, "Todo contains more than one duration.");
        }

        if (matches.Count == 0)
        {
            return new DurationResult(null, null, null);
        }

        if (!int.TryParse(matches[0].Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
            minutes < 15 || minutes > 960 || minutes % 15 != 0)
        {
            return new DurationResult(null, null, "Todo duration must be a 15-minute value from 15m through 960m.");
        }

        return new DurationResult(TimeSpan.FromMinutes(minutes), matches[0].Value, null);
    }

    private static string RemoveRecognizedToken(string text, string? token) =>
        token is null ? text : text.Replace(token, string.Empty, StringComparison.Ordinal);

    private static int CountOccurrences(string content, string value) =>
        (content.Length - content.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;

    private static int IndentWidth(string indent) => indent.Sum(character => character == '\t' ? 4 : 1);

    private static string NormalizeNote(string note) => NoteBulletPattern().Replace(note, string.Empty).Trim();

    [GeneratedRegex("^(#{1,6})\\s+(.+?)\\s*#*\\s*$")]
    private static partial Regex HeadingPattern();

    [GeneratedRegex("^(\\s*)[-*+]\\s+\\[([ xX])\\]\\s*(.*)$")]
    private static partial Regex TaskPattern();

    [GeneratedRegex("^([A-Za-z0-9][A-Za-z0-9._/-]*)\\s+-\\s+")]
    private static partial Regex ReferencePattern();

    [GeneratedRegex("(?<![\\p{L}\\p{N}_])#([\\p{L}\\p{N}_/-]+)")]
    private static partial Regex TagPattern();

    [GeneratedRegex("🛫\\s+(\\d{4}-\\d{2}-\\d{2})")]
    private static partial Regex StartDatePattern();

    [GeneratedRegex("📅\\s+(\\d{4}-\\d{2}-\\d{2})")]
    private static partial Regex DueDatePattern();

    [GeneratedRegex("⏰\\s+(\\d{2}:\\d{2})")]
    private static partial Regex ScheduleTimePattern();

    [GeneratedRegex("⏳\\s+(\\d{4}-\\d{2}-\\d{2})")]
    private static partial Regex ScheduleDatePattern();

    [GeneratedRegex("⏱\\s+(\\d+)m")]
    private static partial Regex DurationPattern();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex("^[-*+]\\s+")]
    private static partial Regex NoteBulletPattern();

    private sealed class TodoBuilder(
        int sourceLine,
        bool isCompleted,
        string? externalReference,
        string title,
        TodoPriority? priority,
        ImmutableArray<string> tags,
        DateOnly? startDate,
        DateOnly? dueDate,
        string sectionPath,
        TodoSchedule? schedule,
        TimeSpan? duration)
    {
        public int Indent { get; set; }

        public List<TodoNote> Notes { get; } = [];

        public List<TodoBuilder> Subtasks { get; } = [];

        public TodoItem Build() => new TodoItem(
            sourceLine,
            isCompleted,
            externalReference,
            title,
            priority,
            tags,
            startDate,
            dueDate,
            sectionPath,
            [.. Notes],
            [.. Subtasks.Select(subtask => subtask.Build())])
        {
            Schedule = schedule,
            Duration = duration
        };
    }

    private sealed record TitleResult(string? Title, int ContentStart, string? Error);

    private sealed record TodoLineResult(TodoBuilder? Builder, string? Error);

    private sealed record DateResult(DateOnly? Date, string? Token, string? Error);

    private sealed record ScheduleResult(
        TodoSchedule? Schedule,
        string? TimeToken,
        string? DateToken,
        string? Error);

    private sealed record DurationResult(TimeSpan? Duration, string? Token, string? Error);
}
