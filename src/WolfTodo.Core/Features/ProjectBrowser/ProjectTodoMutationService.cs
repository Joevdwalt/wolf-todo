using System.Collections.Immutable;
using System.Text.RegularExpressions;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed partial class ProjectTodoMutationService(
    IProjectFileSystem fileSystem,
    MarkdownTodoProjectReader reader)
{
    public TodoMutationResult SetSchedule(
        string path,
        TodoItem expected,
        TodoSchedule? schedule) =>
        MutateExisting(path, expected, todo => Serialize(todo with { Schedule = schedule }));

    public TodoMutationResult SetCompleted(string path, TodoItem expected, bool isCompleted) =>
        MutateExisting(path, expected, todo => Serialize(todo with { IsCompleted = isCompleted }));

    public TodoArchiveResult ArchiveCompleted(string path)
    {
        var archivePath = ArchivePath(path);
        var archivedCount = 0;
        var archiveWritten = false;
        try
        {
            var sourceContents = fileSystem.ReadAllText(path);
            var source = reader.Parse(path, sourceContents);
            if (source.Project is null)
            {
                return TodoArchiveResult.Failure(
                    archivePath,
                    0,
                    source.Error ?? "Project cannot be parsed.");
            }

            var archivedTodos = source.Project.Todos
                .Where(todo => todo.IsCompleted && IsCompletedTree(todo))
                .ToArray();
            if (archivedTodos.Length == 0)
            {
                return TodoArchiveResult.Success(archivePath, 0);
            }

            archivedCount = archivedTodos.Length;

            var sourceLines = SplitLines(sourceContents);
            var blocks = archivedTodos
                .Select(todo =>
                {
                    var sourceIndex = todo.SourceLine - 1;
                    if (sourceIndex < 0 || sourceIndex >= sourceLines.Count)
                    {
                        throw new InvalidDataException("A completed todo no longer exists at its original source line.");
                    }

                    var indent = LeadingWhitespace(sourceLines[sourceIndex]).Length;
                    var end = FindTodoBlockEnd(sourceLines, sourceIndex, indent);
                    return (Start: sourceIndex, End: end, Lines: sourceLines[sourceIndex..end].ToArray());
                })
                .ToArray();

            var archiveContents = fileSystem.FileExists(archivePath)
                ? fileSystem.ReadAllText(archivePath)
                : CreateArchiveDocument(source.Project.Title, DetectNewline(sourceContents));
            var archiveNewline = DetectNewline(archiveContents);
            fileSystem.WriteAllTextAtomically(
                archivePath,
                AppendArchiveBlocks(archiveContents, archiveNewline, blocks.Select(block => block.Lines)));
            archiveWritten = true;

            foreach (var block in blocks.OrderByDescending(block => block.Start))
            {
                sourceLines.RemoveRange(block.Start, block.End - block.Start);
            }

            Write(path, sourceLines, DetectNewline(sourceContents), sourceContents.EndsWith('\n'));
            return TodoArchiveResult.Success(archivePath, archivedCount);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException or InvalidDataException)
        {
            return TodoArchiveResult.Failure(
                archivePath,
                archiveWritten ? archivedCount : 0,
                archiveWritten
                    ? $"Archive copy was written, but the source project was not changed: {exception.Message}"
                    : $"Cannot archive completed todos: {exception.Message}");
        }
    }

    public TodoMutationResult UpdateMany(
        string path,
        IReadOnlyList<TodoItem> expected,
        TodoBulkUpdate update)
    {
        if (expected.Count == 0)
        {
            return TodoMutationResult.Failure("Select at least one todo to update.");
        }

        var validationError = ValidateBulkUpdate(update);
        if (validationError is not null)
        {
            return TodoMutationResult.Failure(validationError);
        }

        if (expected.Select(todo => todo.SourceLine).Distinct().Count() != expected.Count)
        {
            return TodoMutationResult.Failure("The bulk update contains a duplicate todo.");
        }

        try
        {
            var contents = fileSystem.ReadAllText(path);
            var parsed = reader.Parse(path, contents);
            if (parsed.Project is null)
            {
                return TodoMutationResult.Failure(parsed.Error ?? "Project cannot be parsed.");
            }

            var currentByLine = Flatten(parsed.Project.Todos).ToDictionary(todo => todo.SourceLine);
            if (expected.Any(todo =>
                    !currentByLine.TryGetValue(todo.SourceLine, out var current) ||
                    !SameTarget(current, todo)))
            {
                return TodoMutationResult.Failure(
                    "A selected todo changed on disk. Reload it before saving the bulk update.");
            }

            var lines = SplitLines(contents);
            foreach (var expectedTodo in expected)
            {
                var lineIndex = expectedTodo.SourceLine - 1;
                if (lineIndex < 0 || lineIndex >= lines.Count)
                {
                    return TodoMutationResult.Failure(
                        "A selected todo no longer exists at its original source line.");
                }

                var prefix = TaskPrefixPattern().Match(lines[lineIndex]);
                if (!prefix.Success)
                {
                    return TodoMutationResult.Failure(
                        "A selected todo source line is no longer a Markdown task.");
                }

                lines[lineIndex] = prefix.Groups[1].Value + Serialize(
                    ApplyBulkUpdate(currentByLine[expectedTodo.SourceLine], update));
            }

            Write(path, lines, DetectNewline(contents), contents.EndsWith('\n'));
            return TodoMutationResult.Success();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return TodoMutationResult.Failure($"Cannot update project: {exception.Message}");
        }
    }

    public TodoMutationResult RollOverdueToDate(
        string path,
        TodoProject expected,
        DateOnly targetDate)
    {
        try
        {
            var expectedTodos = Flatten(expected.Todos)
                .Where(todo => !todo.IsCompleted && todo.Schedule?.Date < targetDate)
                .ToArray();
            if (expectedTodos.Length == 0)
            {
                return TodoMutationResult.Failure(
                    "The selected project has no incomplete overdue tasks.");
            }

            var contents = fileSystem.ReadAllText(path);
            var parsed = reader.Parse(path, contents);
            if (parsed.Project is null)
            {
                return TodoMutationResult.Failure(parsed.Error ?? "Project cannot be parsed.");
            }

            var currentTodos = Flatten(parsed.Project.Todos)
                .Where(todo => !todo.IsCompleted && todo.Schedule?.Date < targetDate)
                .ToArray();
            var expectedByLine = expectedTodos.ToDictionary(todo => todo.SourceLine);
            if (currentTodos.Length != expectedTodos.Length ||
                currentTodos.Any(todo =>
                    !expectedByLine.TryGetValue(todo.SourceLine, out var expectedTodo) ||
                    !SameTarget(todo, expectedTodo)))
            {
                return TodoMutationResult.Failure(
                    "The project changed on disk. Reload it before rolling tasks to today.");
            }

            var newline = DetectNewline(contents);
            var finalNewline = contents.EndsWith('\n');
            var lines = SplitLines(contents);
            foreach (var todo in currentTodos)
            {
                var lineIndex = todo.SourceLine - 1;
                if (lineIndex < 0 || lineIndex >= lines.Count)
                {
                    return TodoMutationResult.Failure(
                        "An overdue todo no longer exists at its original source line.");
                }

                var prefix = TaskPrefixPattern().Match(lines[lineIndex]);
                if (!prefix.Success)
                {
                    return TodoMutationResult.Failure(
                        "An overdue todo source line is no longer a Markdown task.");
                }

                var schedule = todo.Schedule! with { Date = targetDate };
                lines[lineIndex] = prefix.Groups[1].Value + Serialize(todo with { Schedule = schedule });
            }

            Write(path, lines, newline, finalNewline);
            return TodoMutationResult.Success();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return TodoMutationResult.Failure($"Cannot update project: {exception.Message}");
        }
    }

    public TodoMutationResult Move(string sourcePath, string destinationPath, TodoItem expected)
    {
        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
        {
            return TodoMutationResult.Failure("The todo is already in that project.");
        }

        try
        {
            var sourceContents = fileSystem.ReadAllText(sourcePath);
            var source = reader.Parse(sourcePath, sourceContents);
            if (source.Project is null)
            {
                return TodoMutationResult.Failure(source.Error ?? "Source project cannot be parsed.");
            }

            var current = Flatten(source.Project.Todos).SingleOrDefault(todo => todo.SourceLine == expected.SourceLine);
            if (current is null || !SameTree(current, expected))
            {
                return TodoMutationResult.Failure("The todo changed on disk. Reload it before moving it.");
            }

            var destinationContents = fileSystem.ReadAllText(destinationPath);
            var destination = reader.Parse(destinationPath, destinationContents);
            if (destination.Project is null)
            {
                return TodoMutationResult.Failure(destination.Error ?? "Destination project cannot be parsed.");
            }

            var sourceLines = SplitLines(sourceContents);
            var sourceIndex = expected.SourceLine - 1;
            var sourceIndent = LeadingWhitespace(sourceLines[sourceIndex]);
            var blockEnd = FindTodoBlockEnd(sourceLines, sourceIndex, sourceIndent.Length);
            var block = sourceLines[sourceIndex..blockEnd]
                .Select(line => line.StartsWith(sourceIndent, StringComparison.Ordinal)
                    ? line[sourceIndent.Length..]
                    : line)
                .ToArray();

            var destinationLines = SplitLines(destinationContents);
            var insertionIndex = InboxInsertionIndex(destinationLines);
            destinationLines.InsertRange(insertionIndex, block);

            // Write the destination first: a failed source write may duplicate the todo, but never loses it.
            Write(destinationPath, destinationLines, DetectNewline(destinationContents), finalNewline: true);
            sourceLines.RemoveRange(sourceIndex, blockEnd - sourceIndex);
            Write(sourcePath, sourceLines, DetectNewline(sourceContents), sourceContents.EndsWith('\n'));
            return TodoMutationResult.Success(insertionIndex + 1);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return TodoMutationResult.Failure($"Cannot move todo: {exception.Message}");
        }
    }

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
            var parsed = reader.Parse(path, contents);
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
                Schedule = update.Fields.Schedule,
                Duration = update.Fields.Duration
            });

            foreach (var note in current.Notes)
            {
                var replacement = update.Content.Items
                    .OfType<TodoNoteUpdate>()
                    .FirstOrDefault(candidate => candidate.SourceLine == note.SourceLine);
                if (replacement is null)
                {
                    foreach (var line in Enumerable.Range(note.SourceLine - 1, note.LineCount))
                    {
                        removals.Add(line);
                    }
                    continue;
                }

                replacements[note.SourceLine - 1] = ReplaceNoteText(
                    lines[note.SourceLine - 1],
                    replacement.Text);
                foreach (var line in Enumerable.Range(note.SourceLine, note.LineCount - 1))
                {
                    removals.Add(line);
                }
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
        var result = CreateMany(path, [update]);
        return result.Succeeded
            ? TodoMutationResult.Success(result.SourceLines[0])
            : TodoMutationResult.Failure(result.Error!);
    }

    public TodoBatchMutationResult CreateMany(
        string path,
        IReadOnlyList<TodoTaskUpdate> updates)
    {
        if (updates.Count == 0)
        {
            return TodoBatchMutationResult.Failure("Create at least one todo.");
        }

        for (var index = 0; index < updates.Count; index++)
        {
            var validationError = ValidateNewTodo(updates[index]);
            if (validationError is not null)
            {
                return TodoBatchMutationResult.Failure($"Todo {index + 1}: {validationError}");
            }
        }

        try
        {
            var contents = fileSystem.ReadAllText(path);
            var parsed = reader.Parse(path, contents);
            if (parsed.Project is null)
            {
                return TodoBatchMutationResult.Failure(parsed.Error ?? "Project cannot be parsed.");
            }

            var newline = DetectNewline(contents);
            var lines = SplitLines(contents);
            int insertionIndex;
            try
            {
                insertionIndex = InboxInsertionIndex(lines);
            }
            catch (InvalidDataException exception)
            {
                return TodoBatchMutationResult.Failure(exception.Message);
            }

            var insertedLines = new List<string>();
            var sourceLines = new List<int>(updates.Count);
            foreach (var update in updates)
            {
                sourceLines.Add(insertionIndex + insertedLines.Count + 1);
                var item = new TodoItem(
                    insertionIndex + insertedLines.Count + 1,
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
                    Schedule = update.Fields.Schedule,
                    Duration = update.Fields.Duration
                };
                insertedLines.Add($"- [ ] {SerializeBody(item)}");
                foreach (var content in update.Content.Items)
                {
                    insertedLines.AddRange(SerializeContentItem(content, "  ").Split('\n'));
                }
            }

            lines.InsertRange(insertionIndex, insertedLines);
            Write(path, lines, newline, finalNewline: true);
            return TodoBatchMutationResult.Success(sourceLines);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return TodoBatchMutationResult.Failure($"Cannot update project: {exception.Message}");
        }
    }

    private static string? ValidateNewTodo(TodoTaskUpdate update)
    {
        if (string.IsNullOrWhiteSpace(update.Fields.Title))
        {
            return "Todo title must not be empty.";
        }

        if (update.Fields.Title.IndexOfAny(['\r', '\n']) >= 0)
        {
            return "Todo title must stay on one line.";
        }

        if (update.Fields.ExternalReference?.IndexOfAny([')', '\r', '\n']) >= 0)
        {
            return "External reference must not contain a closing parenthesis or line break.";
        }

        if (update.Fields.Tags.Any(tag =>
                string.IsNullOrWhiteSpace(tag) ||
                tag.TrimStart('#').Length == 0 ||
                tag.Any(char.IsWhiteSpace)))
        {
            return "Tags must be non-empty hashtags without whitespace.";
        }

        if (update.Fields.Schedule?.Time is { } time &&
            (time.Minute is not (0 or 15 or 30 or 45) ||
             time < new TimeOnly(6, 0) ||
             time > new TimeOnly(21, 45)))
        {
            return "Scheduled time must be a quarter-hour from 06:00 through 21:45.";
        }

        if (update.Fields.Duration is { } duration &&
            (duration.TotalMinutes is < 15 or > 960 || duration.TotalMinutes % 15 != 0))
        {
            return "Duration must be a 15-minute value from 15 through 960 minutes.";
        }

        if (update.Content.Items.Any(item => item.SourceLine is not null))
        {
            return "New todo content must not have source identities.";
        }

        if (update.Content.Items.Any(item => item is not TodoNoteUpdate and not TodoSubtaskUpdate))
        {
            return "The new todo content contains an unsupported item.";
        }

        if (update.Content.Items.OfType<TodoNoteUpdate>().Any(note => string.IsNullOrWhiteSpace(note.Text)) ||
            update.Content.Items.OfType<TodoSubtaskUpdate>().Any(subtask =>
                string.IsNullOrWhiteSpace(subtask.Title) ||
                subtask.Title.IndexOfAny(['\r', '\n']) >= 0))
        {
            return "New todo content must not be empty.";
        }

        return null;
    }

    private TodoMutationResult MutateExisting(
        string path,
        TodoItem expected,
        Func<TodoItem, string> serialize)
    {
        try
        {
            var contents = fileSystem.ReadAllText(path);
            var parsed = reader.Parse(path, contents);
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

    private static int InboxInsertionIndex(List<string> lines)
    {
        var inboxes = lines.Select((line, index) => (line, index))
            .Where(candidate => InboxHeadingPattern().IsMatch(candidate.line)).ToArray();
        if (inboxes.Length > 1)
        {
            throw new InvalidDataException("Project contains more than one ## Inbox heading.");
        }

        if (inboxes.Length == 0)
        {
            if (lines.Count > 0 && lines[^1].Length > 0)
            {
                lines.Add(string.Empty);
            }
            lines.Add("## Inbox");
            lines.Add(string.Empty);
            return lines.Count;
        }

        var insertionIndex = FindSectionEnd(lines, inboxes[0].index + 1);
        while (insertionIndex > inboxes[0].index + 1 && lines[insertionIndex - 1].Length == 0)
        {
            insertionIndex--;
        }
        return insertionIndex;
    }

    private static string Serialize(TodoItem todo) =>
        $"{(todo.IsCompleted ? "x" : " ")}] {SerializeBody(todo)}";

    private static string SerializeBody(TodoItem todo)
    {
        var parts = new List<string>();
        if (todo.ExternalReference is not null)
        {
            parts.Add($"({todo.ExternalReference})");
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

        if (todo.Duration is not null)
        {
            parts.Add($"⏱ {(int)todo.Duration.Value.TotalMinutes}m");
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
        current.Duration == expected.Duration &&
        current.Tags.SequenceEqual(expected.Tags, StringComparer.OrdinalIgnoreCase);

    private static bool SameTree(TodoItem current, TodoItem expected) =>
        SameTarget(current, expected) &&
        current.Notes.SequenceEqual(expected.Notes) &&
        current.Subtasks.Length == expected.Subtasks.Length &&
        current.Subtasks.Zip(expected.Subtasks).All(pair => SameTree(pair.First, pair.Second));

    private static string? ValidateBulkUpdate(TodoBulkUpdate update)
    {
        if (!update.HasChanges)
        {
            return "Choose at least one bulk change.";
        }

        if (update.ScheduleMode == TodoBulkScheduleMode.SetDate && update.ScheduledDate is null)
        {
            return "A scheduled date is required when setting the date.";
        }

        if (update.PriorityMode == TodoBulkPriorityMode.Set && update.Priority is null)
        {
            return "A priority is required when setting priority.";
        }

        if (update.TagMode is TodoBulkTagMode.Add or TodoBulkTagMode.Remove &&
            NormalizeTags(update.Tags).Length == 0)
        {
            return "Add and remove tag updates require at least one tag.";
        }

        return null;
    }

    private static TodoItem ApplyBulkUpdate(TodoItem todo, TodoBulkUpdate update)
    {
        var schedule = update.ScheduleMode switch
        {
            TodoBulkScheduleMode.SetDate => new TodoSchedule(update.ScheduledDate!.Value, todo.Schedule?.Time),
            TodoBulkScheduleMode.Clear => null,
            _ => todo.Schedule
        };
        var priority = update.PriorityMode switch
        {
            TodoBulkPriorityMode.Set => update.Priority,
            TodoBulkPriorityMode.Clear => null,
            _ => todo.Priority
        };

        return todo with
        {
            Schedule = schedule,
            Priority = priority,
            Tags = ApplyTagUpdate(todo.Tags, update),
            IsCompleted = update.Complete || todo.IsCompleted
        };
    }

    private static ImmutableArray<string> ApplyTagUpdate(
        ImmutableArray<string> current,
        TodoBulkUpdate update)
    {
        var tags = NormalizeTags(update.Tags);
        return update.TagMode switch
        {
            TodoBulkTagMode.Add =>
                [.. current.Concat(tags.Where(tag => !current.Contains(tag, StringComparer.OrdinalIgnoreCase)))],
            TodoBulkTagMode.Remove =>
                [.. current.Where(tag => !tags.Contains(tag, StringComparer.OrdinalIgnoreCase))],
            TodoBulkTagMode.Replace => tags,
            _ => current
        };
    }

    private static ImmutableArray<string> NormalizeTags(IEnumerable<string> tags) =>
        [.. tags
            .Select(tag => tag.Trim().TrimStart('#'))
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

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

    private static bool IsCompletedTree(TodoItem todo) =>
        todo.IsCompleted && todo.Subtasks.All(IsCompletedTree);

    private static string ArchivePath(string sourcePath)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        var extension = Path.GetExtension(sourcePath);
        return Path.Combine(
            directory,
            $"{Path.GetFileNameWithoutExtension(sourcePath)}.archive" +
            (extension.Length == 0 ? ".md" : extension));
    }

    private static string CreateArchiveDocument(string projectTitle, string newline) =>
        $"# {projectTitle} Archive{newline}{newline}## Archived{newline}";

    private static string AppendArchiveBlocks(
        string archiveContents,
        string newline,
        IEnumerable<IReadOnlyList<string>> blocks)
    {
        var content = archiveContents.TrimEnd('\r', '\n');
        var blockText = string.Join(
            newline + newline,
            blocks.Select(block => string.Join(newline, block).TrimEnd()));
        return content.Length == 0
            ? blockText + newline
            : content + newline + newline + blockText + newline;
    }

    private static string ReplaceNoteText(string line, string text)
    {
        var match = NoteLinePattern().Match(line);
        return match.Success
            ? SerializeNote(text, match.Groups[1].Value + match.Groups[2].Value, LeadingWhitespace(line) + "  ")
            : line;
    }

    private static string LeadingWhitespace(string line) => line[..(line.Length - line.TrimStart().Length)];

    private static string SerializeContentItem(TodoContentItemUpdate item, string indent) => item switch
    {
        TodoNoteUpdate note => SerializeNote(note.Text, $"{indent}- ", indent + "  "),
        TodoSubtaskUpdate subtask =>
            $"{indent}- [{(subtask.IsCompleted ? 'x' : ' ')}] {subtask.Title.Trim()}",
        _ => throw new InvalidOperationException("Unsupported todo content item.")
    };

    private static string SerializeNote(string text, string prefix, string continuationIndent)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return prefix + lines[0].Trim() + string.Concat(lines.Skip(1)
            .Select(line => $"\n{continuationIndent}{line.TrimEnd()}"));
    }

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

    [GeneratedRegex("(?:^|\\s)(?=(?:🔁|➕|✅|❌|🆔|⛔|🏁|⏰|⏱|🛫|⏳|📅|🔺|⏫|🔼|🔽|⏬))")]
    private static partial Regex PreservedTaskMetadataPattern();
}
