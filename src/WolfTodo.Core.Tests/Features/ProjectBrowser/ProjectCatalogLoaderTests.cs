using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Tests.Features.ProjectBrowser;

public sealed class ProjectCatalogLoaderTests
{
    [Fact]
    public void Load_reads_configured_markdown_files_once_and_preserves_source_errors()
    {
        var fileSystem = new FakeProjectFileSystem(
            files: new Dictionary<string, string>
            {
                ["/todos/zeta.md"] = "- [ ] Zeta task",
                ["/todos/alpha.MD"] = "---\ntitle: Alpha\n---\n- [ ] Alpha task"
            });
        var loader = new ProjectCatalogLoader(fileSystem, new ProjectMarkdownParser());

        var result = loader.Load(["/todos/zeta.md", "/todos/alpha.MD", "/todos/zeta.md", "/missing.md"]);

        result.Projects.Select(project => project.Title).Should().Equal("Alpha", "zeta");
        result.Errors.Should().ContainSingle().Which.Path.Should().Be("/missing.md");
    }

    private sealed class FakeProjectFileSystem(
        IReadOnlyDictionary<string, string> files) : IProjectFileSystem
    {
        public bool FileExists(string path) => files.ContainsKey(path);

        public string GetFullPath(string path) => path;

        public string ReadAllText(string path) => files[path];
    }
}
