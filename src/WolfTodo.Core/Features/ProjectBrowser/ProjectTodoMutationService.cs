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
        UpdateTask(path, expected, new TodoTaskUpdate(update, ContentUpdate(expected)));

    public TodoMutationResult UpdateContent(
        string path,
        TodoItem expected,
        TodoContentUpdate update) =>
        UpdateTask(path, expected, new TodoTaskUpdate(FieldUpdate(expected), update));

    public TodoMutationResult UpdateTask(
        string path,
        TodoItem expected,
        TodoTaskUpdate update)
    {
        if (string.IsNullOrWhiteSpace(update.Fields.Title))
        {
            return TodoMutationResult.Failure("Todo title must not be empty.");
        }

        if (update.Content.Items.OfType<TodoNoteUpdate>().Any(note => string.IsNullOrWhiteSpace(note.Text)))
        {
            return TodoMutationResult.Failure("Notes must not be empty.");
        }

        if (update.Content.Items.OfType<TodoSubtaskUpdate>()
            .Any(subtask => string.IsNullOrWhiteSpace(subtask.Title)))
        {
            return TodoMutationResult.Failure("Subtask titles must not be empty.");
        }

        if (update.Content.Items.Any(item => item is not TodoNoteUpdate and not TodoSubtaskUpdate))
        {
            return TodoMutationResult.Failure("The todo content draft contains an unsupported item.");
        }

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
            if (current is null || !SameTree(current, expected))
            {
                return TodoMutationResult.Failure(
                    "The todo content changed on disk. Reload it before saving your change.");
            }

            var currentItems = current.Notes
                .Select(note => (note.SourceLine, IsNote: true))
                .Concat(current.Subtasks.Select(todo => (todo.SourceLine, IsNote: false)))
                .OrderBy(item => item.SourceLine)
                .ToArray();
            var currentByLine = currentItems.ToDictionary(item => item.SourceLine);
            var updatedExisting = update.Content.Items
                .Where(item => item.SourceLine is not null)
                .ToArray();
            var updatedLines = updatedExisting.Select(item => item.SourceLine!.Value).ToArray();
            var retainedLines = updatedLines.ToHashSet();
            var expectedOrder = currentItems
                .Where(item => retainedLines.Contains(item.SourceLine))
                .Select(item => item.SourceLine)
                .ToArray();
            if (updatedLines.Distinct().Count() != updatedLines.Length ||
                updatedExisting.Any(item =>
                    !currentByLine.TryGetValue(item.SourceLine!.Value, out var currentItem) ||
                    currentItem.IsNote != (item is TodoNoteUpdate)) ||
                !updatedLines.SequenceEqual(expectedOrder))
            {
                return TodoMutationResult.Failure("The todo content draft contains stale items.");
            }

            var newline = DetectNewline(contents);
            var finalNewline = contents.EndsWith('\n');
            var lines = SplitLines(contents);
            var targetIndex = expected.SourceLine - 1;
            var targetPrefix = TaskPrefixPattern().Match(lines[targetIndex]);
            if (!targetPrefix.Success)
            {
                return TodoMutationResult.Failure("The todo source line is no longer a Markdown task.");
            }

            var targetIndent = LeadingWhitespace(lines[targetIndex]);
            var childIndent = targetIndent + "  ";
            var blockEnd = FindTodoBlockEnd(lines, targetIndex, targetIndent.Length);
            var replacements = new Dictionary<int, string>();
            var removals = new HashSet<int>();
            var insertions = new Dictionary<int, List<string>>();
            replacements[targetIndex] = targetPrefix.Groups[1].Value + Serialize(current with
            {
                Title = update.Fields.Title.Trim(),
                ExternalReference = NullIfWhiteSpace(update.Fields.ExternalReference),
                Priority = update.Fields.Priority,
                Tags = update.Fields.Tags,
                StartDate = update.Fields.StartDate,
                DueDate = update.Fields.DueDate,
                Schedule = update.Fields.Schedule
            });

            foreach (var note in current.Notes)
            {
                var replacement = update.Content.Items
                    .OfType<TodoNoteUpdate>()
                    .FirstOrDefault(candidate => candidate.SourceLine == note.SourceLine);
                if (replacement is null)
                {
                    removals.Add(note.SourceLine - 1);
                    continue;
                }

                replacements[note.SourceLine - 1] = ReplaceNoteText(
                    lines[note.SourceLine - 1],
                    replacement.Text.Trim());
            }

            foreach (var subtask in current.Subtasks)
            {
                var replacement = update.Content.Items
                    .OfType<TodoSubtaskUpdate>()
                    .FirstOrDefault(candidate => candidate.SourceLine == subtask.SourceLine);
                if (replacement is null)
                {
                    foreach (var line in ContentSourceLines(subtask))
                    {
                        removals.Add(line - 1);
                    }

                    continue;
                }

                var lineIndex = subtask.SourceLine - 1;
                var prefix = TaskPrefixPattern().Match(lines[lineIndex]);
                replacements[lineIndex] = prefix.Groups[1].Value + Serialize(subtask with
                {
                    Title = replacement.Title.Trim(),
                    IsCompleted = replacement.IsCompleted
                });
            }

            var pendingInsertions = new List<string>();
            foreach (var item in update.Content.Items)
            {
                if (item.SourceLine is null)
                {
                    pendingInsertions.Add(SerializeContentItem(item, childIndent));
                    continue;
                }

                if (pendingInsertions.Count > 0)
                {
                    insertions[item.SourceLine.Value - 1] = [.. pendingInsertions];
                    pendingInsertions.Clear();
                }
            }

            if (pendingInsertions.Count > 0)
            {
                insertions[blockEnd] = [.. pendingInsertions];
            }

            var newItemCount = update.Content.Items.Count(item => item.SourceLine is null);
            var output = new List<string>(lines.Count + newItemCount);
            for (var index = 0; index <= lines.Count; index++)
            {
                if (insertions.TryGetValue(index, out var insertedLines))
                {
                    output.AddRange(insertedLines);
                }

                if (index < lines.Count && !removals.Contains(index))
                {
                    output.Add(replacements.GetValueOrDefault(index, lines[index]));
                }
            }

            Write(path, output, newline, finalNewline);
            return TodoMutationResult.Success(expected.SourceLine);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return TodoMutationResult.Failure($"Cannot update project: {exception.Message}");
        }
    }

    public TodoMutationResult Create(string path, TodoUpdate update)
        => Create(path, new TodoTaskUpdate(update, new TodoContentUpdate([])));

    public TodoMutationResult Create(string path, TodoTaskUpdate update)
    {
        if (string.IsNullOrWhiteSpace(update.Fields.Title))
        {
            return TodoMutationResult.Failure("Todo title must not be empty.");
        }

        if (update.Content.Items.Any(item => item.SourceLine is not null))
        {
            return TodoMutationResult.Failure("New todo content must not have source identities.");
        }

        if (update.Content.Items.Any(item => item is not TodoNoteUpdate and not TodoSubtaskUpdate))
        {
            return TodoMutationResult.Failure("The new todo content contains an unsupported item.");
        }

        if (update.Content.Items.OfType<TodoNoteUpdate>().Any(note => string.IsNullOrWhiteSpace(note.Text)) ||
            update.Content.Items.OfType<TodoSubtaskUpdate>()
                .Any(subtask => string.IsNullOrWhiteSpace(subtask.Title)))
        {
            return TodoMutationResult.Failure("New todo content must not be empty.");
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
                NullIfWhiteSpace(update.Fields.ExternalReference),
                update.Fields.Title.Trim(),
                update.Fields.Priority,
                update.Fields.Tags,
                update.Fields.StartDate,
                update.Fields.DueDate,
                "Inbox",
                [],
                [])
            {
                Schedule = update.Fields.Schedule
            };
            lines.Insert(insertionIndex, $"- [ ] {SerializeBody(item)}");
            lines.InsertRange(
                insertionIndex + 1,
                update.Content.Items.Select(content => SerializeContentItem(content, "  ")));
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

        var (description, preservedMetadata) = SplitTitleMetadata(todo.Title);
        if (description.Length > 0)
        {
            parts.Add(description);
        }
        if (todo.Schedule?.Time is not null)
        {
            parts.Add($"⏰ {todo.Schedule.Time.Value:HH:mm}");
        }

        if (preservedMetadata is not null)
        {
            parts.Add(preservedMetadata);
        }

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
            parts.Add($"⏳ {todo.Schedule.Date:yyyy-MM-dd}");
        }

        return string.Join(' ', parts);
    }

    private static (string Description, string? Metadata) SplitTitleMetadata(string title)
    {
        var match = PreservedTaskMetadataPattern().Match(title);
        return !match.Success
            ? (title, null)
            : (title[..match.Index].TrimEnd(), title[match.Index..].TrimStart());
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

    private static bool SameTree(TodoItem current, TodoItem expected) =>
        SameTarget(current, expected) &&
        current.Notes.SequenceEqual(expected.Notes) &&
        current.Subtasks.Length == expected.Subtasks.Length &&
        current.Subtasks.Zip(expected.Subtasks).All(pair => SameTree(pair.First, pair.Second));

    private static IEnumerable<int> ContentSourceLines(TodoItem todo)
    {
        yield return todo.SourceLine;
        foreach (var note in todo.Notes)
        {
            yield return note.SourceLine;
        }

        foreach (var child in todo.Subtasks)
        {
            foreach (var line in ContentSourceLines(child))
            {
                yield return line;
            }
        }
    }

    private static int FindTodoBlockEnd(IReadOnlyList<string> lines, int targetIndex, int targetIndent)
    {
        for (var index = targetIndex + 1; index < lines.Count; index++)
        {
            if (HeadingPattern().IsMatch(lines[index]))
            {
                return index;
            }

            var task = TaskPrefixPattern().Match(lines[index]);
            if (task.Success && LeadingWhitespace(lines[index]).Length <= targetIndent)
            {
                return index;
            }

            if (!string.IsNullOrWhiteSpace(lines[index]) &&
                LeadingWhitespace(lines[index]).Length <= targetIndent)
            {
                return index;
            }
        }

        return lines.Count;
    }

    private static string ReplaceNoteText(string line, string text)
    {
        var match = NoteLinePattern().Match(line);
        return match.Success ? match.Groups[1].Value + match.Groups[2].Value + text : line;
    }

    private static string LeadingWhitespace(string line) => line[..(line.Length - line.TrimStart().Length)];

    private static string SerializeContentItem(TodoContentItemUpdate item, string indent) => item switch
    {
        TodoNoteUpdate note => $"{indent}- {note.Text.Trim()}",
        TodoSubtaskUpdate subtask =>
            $"{indent}- [{(subtask.IsCompleted ? 'x' : ' ')}] {subtask.Title.Trim()}",
        _ => throw new InvalidOperationException("Unsupported todo content item.")
    };

    private static TodoUpdate FieldUpdate(TodoItem todo) => new(
        todo.Title,
        todo.ExternalReference,
        todo.Priority,
        todo.Tags,
        todo.StartDate,
        todo.DueDate,
        todo.Schedule);

    private static TodoContentUpdate ContentUpdate(TodoItem todo) => new(
        [.. todo.Notes
            .Select(note => (TodoContentItemUpdate)new TodoNoteUpdate(note.SourceLine, note.Text))
            .Concat(todo.Subtasks.Select(subtask => (TodoContentItemUpdate)new TodoSubtaskUpdate(
                subtask.SourceLine,
                subtask.Title,
                subtask.IsCompleted)))
            .OrderBy(item => item.SourceLine)]);

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

    [GeneratedRegex("^(\\s*)([-*+]\\s+)?(.*)$")]
    private static partial Regex NoteLinePattern();

    [GeneratedRegex("(?:^|\\s)(?=(?:🔁|➕|✅|❌|🆔|⛔|🏁|⏰|🛫|⏳|📅|🔺|⏫|🔼|🔽|⏬))")]
    private static partial Regex PreservedTaskMetadataPattern();
}
