using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Core.Tests.Infrastructure.Markdown;

public sealed class MarkdownTodoProjectRepositoryTests
{
    [Fact]
    public void Read_canonicalizes_the_path_and_returns_the_parsed_project()
    {
        var fileSystem = new FakeProjectFileSystem(
            "/canonical/work.md",
            new Dictionary<string, string> { ["/canonical/work.md"] = "- [ ] Prepare workshop" });
        var repository = new MarkdownTodoProjectRepository(fileSystem, new MarkdownTodoProjectReader());

        var result = repository.Read("work.md");

        result.IsSuccess.Should().BeTrue();
        result.Path.Should().Be("/canonical/work.md");
        result.Project!.Todos.Single().Title.Should().Be("Prepare workshop");
    }

    [Fact]
    public void Read_reports_a_missing_file()
    {
        var repository = new MarkdownTodoProjectRepository(
            new FakeProjectFileSystem("/canonical/missing.md", new Dictionary<string, string>()),
            new MarkdownTodoProjectReader());

        var result = repository.Read("missing.md");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Project file does not exist: /canonical/missing.md");
    }

    [Fact]
    public void Read_preserves_malformed_markdown_errors()
    {
        var fileSystem = new FakeProjectFileSystem(
            "/canonical/work.md",
            new Dictionary<string, string> { ["/canonical/work.md"] = "- [ ] ⏰ 09:30 ⏳ 2026-07-15" });
        var repository = new MarkdownTodoProjectRepository(fileSystem, new MarkdownTodoProjectReader());

        var result = repository.Read("work.md");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("title must not be empty");
    }

    [Fact]
    public void Read_translates_file_access_failures_to_a_result()
    {
        var repository = new MarkdownTodoProjectRepository(
            new ThrowingProjectFileSystem(),
            new MarkdownTodoProjectReader());

        var result = repository.Read("work.md");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot read project file: access denied");
    }

    private sealed class FakeProjectFileSystem(
        string canonicalPath,
        IReadOnlyDictionary<string, string> files) : IProjectFileSystem
    {
        public bool FileExists(string path) => files.ContainsKey(path);

        public string GetFullPath(string path) => canonicalPath;

        public string ReadAllText(string path) => files[path];
    }

    private sealed class ThrowingProjectFileSystem : IProjectFileSystem
    {
        public bool FileExists(string path) => true;

        public string GetFullPath(string path) => "/canonical/work.md";

        public string ReadAllText(string path) => throw new IOException("access denied");
    }
}
