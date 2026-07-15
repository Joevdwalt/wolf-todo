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
            "## Work\r\n\r\n- [ ] Prepare proposal #work ⏳ 2026-07-15 ⏰ 09:30\r\n  - note\r\n");
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
    public void UpdateContent_edits_and_adds_direct_content_without_rewriting_descendants()
    {
        const string path = "/todos/work.md";
        const string markdown = "- [ ] Parent\n  - old note\n  - [ ] Child #tag\n    - nested note\n- [ ] Sibling\n";
        var parser = new ProjectMarkdownParser();
        var expected = parser.Parse(path, markdown).Project!.Todos[0];
        var fileSystem = new WritableFileSystem(path, markdown);
        var service = new ProjectTodoMutationService(fileSystem, parser);

        var result = service.UpdateContent(path, expected, new TodoContentUpdate(
            [new TodoNoteUpdate(2, "updated note"), new TodoNoteUpdate(null, "new note")],
            [new TodoSubtaskUpdate(3, "Changed child", true),
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
            new TodoContentUpdate([], []));

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
            new TodoContentUpdate([], [new TodoSubtaskUpdate(2, "Child", false)]));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("changed on disk");
        fileSystem.Contents.Should().Be(changed);
    }

    private sealed class WritableFileSystem(string path, string contents) : IProjectFileSystem
    {
        public string Contents { get; private set; } = contents;

        public bool FileExists(string candidate) => candidate == path;

        public string GetFullPath(string candidate) => candidate;

        public string ReadAllText(string candidate) =>
            candidate == path ? Contents : throw new FileNotFoundException();

        public void WriteAllTextAtomically(string candidate, string value)
        {
            candidate.Should().Be(path);
            Contents = value;
        }
    }
}
