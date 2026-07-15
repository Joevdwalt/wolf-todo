using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed partial class ProjectTodoMutationService(
    IProjectFileSystem fileSystem,
    ProjectMarkdownParser parser)
{
    public TodoMutationResult SetSchedule(
        string path,
        TodoItem expected,
        TodoSchedule? schedule) =>
        MutateExisting(path, expected, todo => Serialize(todo with { Schedule = schedule }));

    public TodoMutationResult SetCompleted(string path, TodoItem expected, bool isCompleted) =>
        MutateExisting(path, expected, todo => Serialize(todo with { IsCompleted = isCompleted }));

    public TodoMutationResult Update(string path, TodoItem expected, TodoUpdate update) =>
        MutateExisting(path, expected, todo => Serialize(todo with
        {
            Title = update.Title.Trim(),
            ExternalReference = NullIfWhiteSpace(update.ExternalReference),
            Priority = update.Priority,
            Tags = update.Tags,
            StartDate = update.StartDate,
            DueDate = update.DueDate
        }));

    public TodoMutationResult Create(
        string path,
        TodoUpdate update,
        TodoSchedule? schedule = null)
    {
        if (string.IsNullOrWhiteSpace(update.Title))
        {
            return TodoMutationResult.Failure("Todo title must not be empty.");
        }

        try
        {
            var contents = fileSystem.ReadAllText(path);
            var parsed = parser.Parse(path, contents);
            if (parsed.Project is null)
            {
                return TodoMutationResult.Failure(parsed.Error ?? "Project cannot be parsed.");
            }

            var newline = DetectNewline(contents);
            var lines = SplitLines(contents);
            var inboxes = lines
                .Select((line, index) => (line, index))
                .Where(candidate => InboxHeadingPattern().IsMatch(candidate.line))
                .ToArray();

            if (inboxes.Length > 1)
            {
                return TodoMutationResult.Failure("Project contains more than one ## Inbox heading.");
            }

            var insertionIndex = lines.Count;
            if (inboxes.Length == 0)
            {
                if (lines.Count > 0 && lines[^1].Length > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("## Inbox");
                lines.Add(string.Empty);
                insertionIndex = lines.Count;
            }
            else
            {
                insertionIndex = FindSectionEnd(lines, inboxes[0].index + 1);
                while (insertionIndex > inboxes[0].index + 1 && lines[insertionIndex - 1].Length == 0)
                {
                    insertionIndex--;
                }
            }

            var item = new TodoItem(
                insertionIndex + 1,
                false,
                NullIfWhiteSpace(update.ExternalReference),
                update.Title.Trim(),
                update.Priority,
                update.Tags,
                update.StartDate,
                update.DueDate,
                "Inbox",
                [],
                [])
            {
                Schedule = schedule
            };
            lines.Insert(insertionIndex, $"- [ ] {SerializeBody(item)}");
            Write(path, lines, newline, finalNewline: true);
            return TodoMutationResult.Success(insertionIndex + 1);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return TodoMutationResult.Failure($"Cannot update project: {exception.Message}");
        }
    }

    private TodoMutationResult MutateExisting(
        string path,
        TodoItem expected,
        Func<TodoItem, string> serialize)
    {
        try
        {
            var contents = fileSystem.ReadAllText(path);
            var parsed = parser.Parse(path, contents);
            if (parsed.Project is null)
            {
                return TodoMutationResult.Failure(parsed.Error ?? "Project cannot be parsed.");
            }

            var current = Flatten(parsed.Project.Todos)
                .SingleOrDefault(todo => todo.SourceLine == expected.SourceLine);
            if (current is null || !SameTarget(current, expected))
            {
                return TodoMutationResult.Failure(
                    "The todo changed on disk. Reload it before saving your change.");
            }

            var newline = DetectNewline(contents);
            var finalNewline = contents.EndsWith('\n');
            var lines = SplitLines(contents);
            var lineIndex = expected.SourceLine - 1;
            if (lineIndex < 0 || lineIndex >= lines.Count)
            {
                return TodoMutationResult.Failure("The todo no longer exists at its original source line.");
            }

            var prefix = TaskPrefixPattern().Match(lines[lineIndex]);
            if (!prefix.Success)
            {
                return TodoMutationResult.Failure("The todo source line is no longer a Markdown task.");
            }

            lines[lineIndex] = prefix.Groups[1].Value + serialize(current);
            Write(path, lines, newline, finalNewline);
            return TodoMutationResult.Success(expected.SourceLine);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return TodoMutationResult.Failure($"Cannot update project: {exception.Message}");
        }
    }

    private void Write(
        string path,
        IReadOnlyList<string> lines,
        string newline,
        bool finalNewline)
    {
        var contents = string.Join(newline, lines);
        if (finalNewline && !contents.EndsWith(newline, StringComparison.Ordinal))
        {
            contents += newline;
        }

        fileSystem.WriteAllTextAtomically(path, contents);
    }

    private static string Serialize(TodoItem todo) =>
        $"{(todo.IsCompleted ? "x" : " ")}] {SerializeBody(todo)}";

    private static string SerializeBody(TodoItem todo)
    {
        var parts = new List<string>();
        if (todo.ExternalReference is not null)
        {
            parts.Add($"{todo.ExternalReference} -");
        }

        parts.Add(todo.Title);
        var priority = todo.Priority switch
        {
            TodoPriority.Highest => "🔺",
            TodoPriority.High => "⏫",
            TodoPriority.Medium => "🔼",
            TodoPriority.Low => "🔽",
            TodoPriority.Lowest => "⏬",
            _ => null
        };
        if (priority is not null)
        {
            parts.Add(priority);
        }

        parts.AddRange(todo.Tags.Select(tag => $"#{tag.TrimStart('#')}"));
        if (todo.StartDate is not null)
        {
            parts.Add($"🛫 {todo.StartDate:yyyy-MM-dd}");
        }

        if (todo.DueDate is not null)
        {
            parts.Add($"📅 {todo.DueDate:yyyy-MM-dd}");
        }

        if (todo.Schedule is not null)
        {
            parts.Add($"⏳ {todo.Schedule.Date:yyyy-MM-dd} ⏰ {todo.Schedule.Time:HH:mm}");
        }

        return string.Join(' ', parts);
    }

    private static bool SameTarget(TodoItem current, TodoItem expected) =>
        current.SourceLine == expected.SourceLine &&
        current.IsCompleted == expected.IsCompleted &&
        current.Title == expected.Title &&
        current.ExternalReference == expected.ExternalReference &&
        current.Priority == expected.Priority &&
        current.StartDate == expected.StartDate &&
        current.DueDate == expected.DueDate &&
        current.Schedule == expected.Schedule &&
        current.Tags.SequenceEqual(expected.Tags, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<TodoItem> Flatten(IEnumerable<TodoItem> todos)
    {
        foreach (var todo in todos)
        {
            yield return todo;
            foreach (var subtask in Flatten(todo.Subtasks))
            {
                yield return subtask;
            }
        }
    }

    private static int FindSectionEnd(IReadOnlyList<string> lines, int start)
    {
        for (var index = start; index < lines.Count; index++)
        {
            var heading = HeadingPattern().Match(lines[index]);
            if (heading.Success && heading.Groups[1].Value.Length <= 2)
            {
                return index;
            }
        }

        return lines.Count;
    }

    private static List<string> SplitLines(string contents)
    {
        var lines = contents.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
        if (contents.EndsWith('\n') && lines.Count > 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }

    private static string DetectNewline(string contents) =>
        contents.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("^(\\s*[-*+]\\s+\\[)[ xX]\\]\\s*")]
    private static partial Regex TaskPrefixPattern();

    [GeneratedRegex("^\\s*##\\s+Inbox\\s*#*\\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex InboxHeadingPattern();

    [GeneratedRegex("^(#{1,6})\\s+")]
    private static partial Regex HeadingPattern();
}
