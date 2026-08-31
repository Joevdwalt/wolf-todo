using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Core.Tests.Features.ProjectBrowser;

public sealed class ProjectTodoMutationServiceTests
{
    [Fact]
    public void UpdateMany_applies_shared_fields_to_multiple_tasks_in_one_write()
    {
        const string path = "/todos/work.md";
        const string markdown =
            "- [ ] Timed ⏰ 09:30 #work ⏳ 2026-08-01\r\n" +
            "- [x] All day #home\r\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos;
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateMany(
            path,
            expected,
            new TodoBulkUpdate(
                TodoBulkScheduleMode.SetDate,
                new DateOnly(2026, 8, 20),
                TodoBulkTagMode.Add,
                ["focus", "WORK"],
                TodoBulkPriorityMode.Set,
                TodoPriority.High,
                Complete: true));

        result.Succeeded.Should().BeTrue();
        fileSystem.WriteCount.Should().Be(1);
        fileSystem.Contents.Should().Be(
            "- [x] Timed ⏰ 09:30 ⏫ #work #focus ⏳ 2026-08-20\r\n" +
            "- [x] All day ⏫ #home #focus #WORK ⏳ 2026-08-20\r\n");
    }

    [Fact]
    public void UpdateMany_replaces_or_clears_tags_and_schedule()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Task #one #two ⏳ 2026-08-01\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos;
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateMany(
            path,
            expected,
            new TodoBulkUpdate(
                TodoBulkScheduleMode.Clear,
                null,
                TodoBulkTagMode.Replace,
                [],
                TodoBulkPriorityMode.Clear,
                null,
                Complete: false));

        result.Succeeded.Should().BeTrue();
        fileSystem.Contents.Should().Be("- [ ] Task\n");
    }

    [Fact]
    public void UpdateMany_rejects_the_complete_project_group_when_one_target_is_stale()
    {
        const string path = "/todos/work.md";
        const string original = "- [ ] First\n- [ ] Second\n";
        const string changed = "- [ ] First\n- [ ] Changed externally\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, original).Project!.Todos;
        var fileSystem = new WritableFileSystem(path, changed);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateMany(
            path,
            expected,
            new TodoBulkUpdate(
                TodoBulkScheduleMode.Unchanged,
                null,
                TodoBulkTagMode.Unchanged,
                [],
                TodoBulkPriorityMode.Set,
                TodoPriority.High,
                Complete: false));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("changed on disk");
        fileSystem.WriteCount.Should().Be(0);
        fileSystem.Contents.Should().Be(changed);
    }

    [Fact]
    public void RollOverdueToDate_updates_incomplete_root_and_nested_tasks_in_one_write()
    {
        const string path = "/todos/work.md";
        const string markdown =
            "## Work\r\n\r\n" +
            "- [ ] Timed ⏰ 09:30 ⏱ 30m #work ⏳ 2026-07-20\r\n" +
            "  - [ ] Nested ⏳ 2026-07-21\r\n" +
            "- [x] Completed ⏳ 2026-07-20\r\n" +
            "- [ ] Today ⏳ 2026-07-23\r\n" +
            "- [ ] Future ⏳ 2026-07-24\r\n" +
            "- [ ] Unscheduled\r\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!;
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.RollOverdueToDate(
            path,
            expected,
            new DateOnly(2026, 7, 23));

        result.Succeeded.Should().BeTrue();
        result.SourceLine.Should().BeNull();
        fileSystem.WriteCount.Should().Be(1);
        fileSystem.Contents.Should().Be(
            "## Work\r\n\r\n" +
            "- [ ] Timed ⏰ 09:30 ⏱ 30m #work ⏳ 2026-07-23\r\n" +
            "  - [ ] Nested ⏳ 2026-07-23\r\n" +
            "- [x] Completed ⏳ 2026-07-20\r\n" +
            "- [ ] Today ⏳ 2026-07-23\r\n" +
            "- [ ] Future ⏳ 2026-07-24\r\n" +
            "- [ ] Unscheduled\r\n");
    }

    [Fact]
    public void RollOverdueToDate_refuses_a_changed_eligible_set_without_writing()
    {
        const string path = "/todos/work.md";
        const string original = "- [ ] Original ⏳ 2026-07-20\n";
        const string changed =
            "- [ ] Original ⏳ 2026-07-20\n" +
            "- [ ] Added externally ⏳ 2026-07-21\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, original).Project!;
        var fileSystem = new WritableFileSystem(path, changed);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.RollOverdueToDate(
            path,
            expected,
            new DateOnly(2026, 7, 23));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("changed on disk");
        fileSystem.WriteCount.Should().Be(0);
        fileSystem.Contents.Should().Be(changed);
    }

    [Fact]
    public void RollOverdueToDate_reports_when_no_tasks_are_eligible()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Today ⏳ 2026-07-23\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!;
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.RollOverdueToDate(
            path,
            expected,
            new DateOnly(2026, 7, 23));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("no incomplete overdue tasks");
        fileSystem.WriteCount.Should().Be(0);
    }

    [Fact]
    public void SetSchedule_updates_only_the_target_line_and_preserves_newlines()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "## Work\r\n\r\n- [ ] Prepare proposal #work\r\n  - note\r\n");
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, fileSystem.Contents).Project!.Todos.Single();
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.SetSchedule(
            path,
            expected,
            new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 30)));

        result.Succeeded.Should().BeTrue();
        fileSystem.Contents.Should().Be(
            "## Work\r\n\r\n- [ ] Prepare proposal ⏰ 09:30 #work ⏳ 2026-07-15\r\n  - note\r\n");
    }

    [Fact]
    public void SetSchedule_refuses_a_stale_todo()
    {
        const string path = "/todos/work.md";
        var parser = new MarkdownTodoProjectReader();
        var original = "- [ ] Prepare proposal";
        var expected = parser.Parse(path, original).Project!.Todos.Single();
        var fileSystem = new WritableFileSystem(path, "- [ ] Externally changed");
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.SetSchedule(
            path,
            expected,
            new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 30)));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("changed on disk");
        fileSystem.Contents.Should().Be("- [ ] Externally changed");
    }

    [Fact]
    public void SetSchedule_writes_an_all_day_schedule_without_a_clock()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "- [ ] Prepare proposal #work\n");
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, fileSystem.Contents).Project!.Todos.Single();
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.SetSchedule(path, expected, new TodoSchedule(new DateOnly(2026, 7, 15)));

        result.Succeeded.Should().BeTrue();
        fileSystem.Contents.Should().Be("- [ ] Prepare proposal #work ⏳ 2026-07-15\n");
    }

    [Fact]
    public void Create_adds_an_inbox_and_returns_the_new_source_line()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "# Work\n");
        var service = new ProjectTodoMutationService(fileSystem, new MarkdownTodoProjectReader());

        var result = service.Create(
            path,
            new TodoUpdate("New task", null, TodoPriority.High, ["now"], null, null));

        result.Succeeded.Should().BeTrue();
        result.SourceLine.Should().Be(5);
        fileSystem.Contents.Should().Be("# Work\n\n## Inbox\n\n- [ ] New task ⏫ #now\n");
    }

    [Fact]
    public void Update_changes_schedule_and_preserves_legacy_start_and_due_metadata()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Existing 🛫 2026-07-01 📅 2026-07-31\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos.Single();
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);
        var schedule = new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 30));

        var result = service.Update(
            path,
            expected,
            new TodoUpdate(
                "Changed",
                null,
                null,
                [],
                expected.StartDate,
                expected.DueDate,
                schedule));

        result.Succeeded.Should().BeTrue();
        fileSystem.Contents.Should().Be(
            "- [ ] Changed ⏰ 09:30 🛫 2026-07-01 📅 2026-07-31 ⏳ 2026-07-15\n");
    }

    [Fact]
    public void Update_normalizes_legacy_schedule_before_preserved_and_structured_task_markers()
    {
        const string path = "/todos/work.md";
        const string markdown =
            "- [ ] Existing 🔁 every day ➕ 2026-07-01 ⏫ #work ⏳ 2026-07-15 ⏰ 09:30\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos.Single();
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.Update(path, expected, new TodoUpdate(
            expected.Title,
            expected.ExternalReference,
            expected.Priority,
            expected.Tags,
            expected.StartDate,
            expected.DueDate,
            expected.Schedule));

        result.Succeeded.Should().BeTrue();
        fileSystem.Contents.Should().Be(
            "- [ ] Existing ⏰ 09:30 🔁 every day ➕ 2026-07-01 ⏫ #work ⏳ 2026-07-15\n");
    }

    [Fact]
    public void UpdateTask_changes_fields_and_ordered_content_in_one_atomic_write()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Parent\n  - old note\n  - [ ] Child\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos.Single();
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateTask(
            path,
            expected,
            new TodoTaskUpdate(
                new TodoUpdate("Renamed", "EXT-7", TodoPriority.High, ["now"], null, null),
                new TodoContentUpdate([
                    new TodoNoteUpdate(2, "updated note"),
                    new TodoNoteUpdate(null, "inserted note"),
                    new TodoSubtaskUpdate(3, "Changed child", true)])));

        result.Succeeded.Should().BeTrue();
        fileSystem.WriteCount.Should().Be(1);
        fileSystem.Contents.Should().Be(
            "- [ ] (EXT-7) Renamed ⏫ #now\n" +
            "  - updated note\n" +
            "  - inserted note\n" +
            "  - [x] Changed child\n");
        parser.Parse(path, fileSystem.Contents).Project!.Todos.Single().ExternalReference.Should().Be("EXT-7");
    }

    [Fact]
    public void UpdateTask_replaces_the_full_multiline_note_block()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Parent\n  - old first\n    old continuation\n  - [ ] Child\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos.Single();
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateTask(
            path,
            expected,
            new TodoTaskUpdate(
                new TodoUpdate("Parent", null, null, [], null, null),
                new TodoContentUpdate([
                    new TodoNoteUpdate(2, "new first\n\nnew second"),
                    new TodoSubtaskUpdate(4, "Child", false)])));

        result.Succeeded.Should().BeTrue();
        fileSystem.Contents.Should().Be(
            "- [ ] Parent\n" +
            "  - new first\n" +
            "    \n" +
            "    new second\n" +
            "  - [ ] Child\n");
    }

    [Fact]
    public void Create_writes_fields_and_interleaved_content_together()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "## Inbox\n");
        var service = new ProjectTodoMutationService(fileSystem, new MarkdownTodoProjectReader());

        var result = service.Create(
            path,
            new TodoTaskUpdate(
                new TodoUpdate("New task", null, null, [], null, null),
                new TodoContentUpdate([
                    new TodoNoteUpdate(null, "context"),
                    new TodoSubtaskUpdate(null, "first step", false),
                    new TodoNoteUpdate(null, "closing note")])));

        result.Succeeded.Should().BeTrue();
        fileSystem.WriteCount.Should().Be(1);
        fileSystem.Contents.Should().Be(
            "## Inbox\n- [ ] New task\n" +
            "  - context\n" +
            "  - [ ] first step\n" +
            "  - closing note\n");
    }

    [Fact]
    public void CreateMany_writes_the_complete_batch_once_and_returns_source_lines()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "# Work\n");
        var service = new ProjectTodoMutationService(fileSystem, new MarkdownTodoProjectReader());

        var result = service.CreateMany(path,
        [
            new TodoTaskUpdate(
                new TodoUpdate("First", null, TodoPriority.High, ["now"], null, null),
                new TodoContentUpdate([new TodoNoteUpdate(null, "context\ncontinued")])),
            new TodoTaskUpdate(
                new TodoUpdate("Second", "EXT-2", null, [], null, null),
                new TodoContentUpdate([new TodoSubtaskUpdate(null, "step", true)]))
        ]);

        result.Succeeded.Should().BeTrue();
        result.SourceLines.Should().Equal(5, 8);
        fileSystem.WriteCount.Should().Be(1);
        fileSystem.Contents.Should().Be(
            "# Work\n\n## Inbox\n\n" +
            "- [ ] First ⏫ #now\n" +
            "  - context\n" +
            "    continued\n" +
            "- [ ] (EXT-2) Second\n" +
            "  - [x] step\n");
    }

    [Fact]
    public void CreateMany_rejects_any_invalid_item_without_writing()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "# Work\n");
        var service = new ProjectTodoMutationService(fileSystem, new MarkdownTodoProjectReader());

        var result = service.CreateMany(path,
        [
            new TodoTaskUpdate(
                new TodoUpdate("Valid", null, null, [], null, null),
                new TodoContentUpdate([])),
            new TodoTaskUpdate(
                new TodoUpdate(" ", null, null, [], null, null),
                new TodoContentUpdate([]))
        ]);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().StartWith("Todo 2:");
        fileSystem.WriteCount.Should().Be(0);
        fileSystem.Contents.Should().Be("# Work\n");
    }

    [Fact]
    public void CreateMany_rejects_an_empty_batch_without_reading_or_writing()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "# Work\n");

        var result = new ProjectTodoMutationService(fileSystem, new MarkdownTodoProjectReader())
            .CreateMany(path, []);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Create at least one todo.");
        fileSystem.WriteCount.Should().Be(0);
    }

    [Fact]
    public void UpdateContent_edits_and_adds_direct_content_without_rewriting_descendants()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Parent\n  - old note\n  - [ ] Child #tag\n    - nested note\n- [ ] Sibling\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos[0];
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateContent(path, expected, new TodoContentUpdate(
            [new TodoNoteUpdate(2, "updated note"),
             new TodoNoteUpdate(null, "new note"),
             new TodoSubtaskUpdate(3, "Changed child", true),
             new TodoSubtaskUpdate(null, "Second child", false)]));

        result.Succeeded.Should().BeTrue();
        fileSystem.Contents.Should().Be(
            "- [ ] Parent\n" +
            "  - updated note\n" +
            "  - new note\n" +
            "  - [x] Changed child #tag\n" +
            "    - nested note\n" +
            "  - [ ] Second child\n" +
            "- [ ] Sibling\n");
    }

    [Fact]
    public void UpdateContent_inserts_new_content_at_its_ordered_outline_position()
    {
        const string path = "/todos/work.md";
        const string markdown =
            "- [ ] Parent\n" +
            "  - opening note\n" +
            "  - [ ] Child\n" +
            "    - nested note\n" +
            "  - closing note\n" +
            "- [ ] Sibling\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos[0];
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateContent(path, expected, new TodoContentUpdate(
            [new TodoNoteUpdate(2, "opening note"),
             new TodoSubtaskUpdate(3, "Child", false),
             new TodoNoteUpdate(null, "inserted after child"),
             new TodoNoteUpdate(5, "closing note")]));

        result.Succeeded.Should().BeTrue();
        fileSystem.Contents.Should().Be(
            "- [ ] Parent\n" +
            "  - opening note\n" +
            "  - [ ] Child\n" +
            "    - nested note\n" +
            "  - inserted after child\n" +
            "  - closing note\n" +
            "- [ ] Sibling\n");
    }

    [Fact]
    public void UpdateContent_rejects_reordered_or_retyped_source_items()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Parent\n  - note\n  - [ ] Child\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos[0];
        var reorderedFile = new WritableFileSystem(path, markdown);
        var retypedFile = new WritableFileSystem(path, markdown);

        var reordered = new ProjectTodoMutationService(reorderedFile, parser).UpdateContent(
            path,
            expected,
            new TodoContentUpdate(
                [new TodoSubtaskUpdate(3, "Child", false), new TodoNoteUpdate(2, "note")]));
        var retyped = new ProjectTodoMutationService(retypedFile, parser).UpdateContent(
            path,
            expected,
            new TodoContentUpdate(
                [new TodoSubtaskUpdate(2, "Not a note", false), new TodoSubtaskUpdate(3, "Child", false)]));

        reordered.Succeeded.Should().BeFalse();
        reordered.Error.Should().Contain("stale items");
        reorderedFile.Contents.Should().Be(markdown);
        retyped.Succeeded.Should().BeFalse();
        retyped.Error.Should().Contain("stale items");
        retypedFile.Contents.Should().Be(markdown);
    }

    [Fact]
    public void UpdateContent_removes_a_subtask_and_its_descendant_content()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Parent\n  - [ ] Child\n    - child note\n    - [ ] Grandchild\n- [ ] Sibling\n";
        var parser = new MarkdownTodoProjectReader();
        var expected = parser.Parse(path, markdown).Project!.Todos[0];
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateContent(
            path,
            expected,
            new TodoContentUpdate([]));

        result.Succeeded.Should().BeTrue();
        fileSystem.Contents.Should().Be("- [ ] Parent\n- [ ] Sibling\n");
    }

    [Fact]
    public void UpdateContent_refuses_a_stale_nested_note_without_writing()
    {
        const string path = "/todos/work.md";
        var parser = new MarkdownTodoProjectReader();
        var original = "- [ ] Parent\n  - [ ] Child\n    - original note\n";
        var expected = parser.Parse(path, original).Project!.Todos[0];
        var changed = "- [ ] Parent\n  - [ ] Child\n    - externally changed\n";
        var fileSystem = new WritableFileSystem(path, changed);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateContent(
            path,
            expected,
            new TodoContentUpdate([new TodoSubtaskUpdate(2, "Child", false)]));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("changed on disk");
        fileSystem.Contents.Should().Be(changed);
    }

    [Fact]
    public void ArchiveCompleted_moves_only_completed_top_level_trees_to_a_companion_file()
    {
        const string path = "/todos/work.md";
        const string archivePath = "/todos/work.archive.md";
        const string markdown =
            "# Work\n\n" +
            "- [x] Finished\n  - completed note\n  - [x] Finished child\n" +
            "- [x] Parent with open child\n  - [ ] Still active\n" +
            "- [ ] Open task\n";
        var fileSystem = new ArchiveFileSystem(path, markdown);
        var result = new ProjectTodoMutationService(fileSystem, new MarkdownTodoProjectReader())
            .ArchiveCompleted(path);

        result.Succeeded.Should().BeTrue();
        result.ArchivedCount.Should().Be(1);
        result.ArchivePath.Should().Be(archivePath);
        fileSystem.Files[path].Should()
            .NotContain("Finished\n  - completed note")
            .And.Contain("Parent with open child")
            .And.Contain("Open task");
        fileSystem.Files[archivePath].Should()
            .Contain("# work Archive")
            .And.Contain("## Archived")
            .And.Contain("- [x] Finished\n  - completed note\n  - [x] Finished child");
    }

    [Fact]
    public void ArchiveCompleted_appends_to_an_existing_companion_file()
    {
        const string path = "/todos/work.md";
        const string archivePath = "/todos/work.archive.md";
        var fileSystem = new ArchiveFileSystem(path, "# Work\n\n- [x] Finished\n");
        fileSystem.Files[archivePath] = "# Work Archive\n\n## Archived\n\n- [x] Previous\n";

        var result = new ProjectTodoMutationService(fileSystem, new MarkdownTodoProjectReader())
            .ArchiveCompleted(path);

        result.Succeeded.Should().BeTrue();
        fileSystem.Files[archivePath].Should().Contain("- [x] Previous").And.Contain("- [x] Finished");
        fileSystem.Files[archivePath].Split("## Archived", StringSplitOptions.None).Should().HaveCount(2);
    }

    [Fact]
    public void ArchiveCompleted_keeps_the_source_when_removing_it_fails_after_writing_the_archive()
    {
        const string path = "/todos/work.md";
        var fileSystem = new ArchiveFileSystem(path, "# Work\n\n- [x] Finished\n")
        {
            FailSourceWrites = true
        };

        var result = new ProjectTodoMutationService(fileSystem, new MarkdownTodoProjectReader())
            .ArchiveCompleted(path);

        result.Succeeded.Should().BeFalse();
        result.ArchivedCount.Should().Be(1);
        result.Error.Should().Contain("Archive copy was written");
        fileSystem.Files[path].Should().Contain("- [x] Finished");
        fileSystem.Files["/todos/work.archive.md"].Should().Contain("- [x] Finished");
    }

    private sealed class WritableFileSystem(string path, string contents) : IProjectFileSystem
    {
        public string Contents { get; private set; } = contents;

        public int WriteCount { get; private set; }

        public bool FileExists(string candidate) => candidate == path;

        public string GetFullPath(string candidate) => candidate;

        public string ReadAllText(string candidate) =>
            candidate == path ? Contents : throw new FileNotFoundException();

        public void WriteAllTextAtomically(string candidate, string value)
        {
            candidate.Should().Be(path);
            Contents = value;
            WriteCount++;
        }
    }

    private sealed class ArchiveFileSystem(string path, string contents) : IProjectFileSystem
    {
        public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal)
        {
            [path] = contents
        };

        public bool FailSourceWrites { get; init; }

        public bool FileExists(string candidate) => Files.ContainsKey(candidate);

        public string GetFullPath(string candidate) => candidate;

        public string ReadAllText(string candidate) => Files.TryGetValue(candidate, out var value)
            ? value
            : throw new FileNotFoundException();

        public void WriteAllTextAtomically(string candidate, string value)
        {
            if (FailSourceWrites && candidate == path)
            {
                throw new IOException("disk full");
            }

            Files[candidate] = value;
        }
    }
}
