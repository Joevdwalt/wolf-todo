using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Tests.Features.ProjectBrowser;

public sealed class ProjectTodoMutationServiceTests
{
    [Fact]
    public void SetSchedule_updates_only_the_target_line_and_preserves_newlines()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "## Work\r\n\r\n- [ ] Prepare proposal #work\r\n  - note\r\n");
        var parser = new ProjectMarkdownParser();
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
        var parser = new ProjectMarkdownParser();
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
    public void Create_adds_an_inbox_and_returns_the_new_source_line()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "# Work\n");
        var service = new ProjectTodoMutationService(fileSystem, new ProjectMarkdownParser());

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
        var parser = new ProjectMarkdownParser();
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
        var parser = new ProjectMarkdownParser();
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
        var parser = new ProjectMarkdownParser();
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
            "- [ ] EXT-7 - Renamed ⏫ #now\n" +
            "  - updated note\n" +
            "  - inserted note\n" +
            "  - [x] Changed child\n");
    }

    [Fact]
    public void Create_writes_fields_and_interleaved_content_together()
    {
        const string path = "/todos/work.md";
        var fileSystem = new WritableFileSystem(path, "## Inbox\n");
        var service = new ProjectTodoMutationService(fileSystem, new ProjectMarkdownParser());

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
    public void UpdateContent_edits_and_adds_direct_content_without_rewriting_descendants()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Parent\n  - old note\n  - [ ] Child #tag\n    - nested note\n- [ ] Sibling\n";
        var parser = new ProjectMarkdownParser();
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
        var parser = new ProjectMarkdownParser();
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
        var parser = new ProjectMarkdownParser();
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
        var parser = new ProjectMarkdownParser();
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
        var parser = new ProjectMarkdownParser();
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
}
