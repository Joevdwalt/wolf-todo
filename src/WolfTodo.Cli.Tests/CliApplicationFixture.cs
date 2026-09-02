using WolfTodo.Cli;
using WolfTodo.Cli.Features;
using WolfTodo.Cli.Infrastructure;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Cli.Tests;

public sealed class CliApplicationFixture
{
    private const string ProjectPath = "/todos/work.md";
    private const string Markdown = "---\ntitle: Work\n---\n\n# Work\n";
    private readonly StringReader input;

    public CliApplicationFixture(string stdin = "", string? markdown = null)
    {
        input = new StringReader(stdin);
        FileSystem = new MemoryProjectFileSystem(ProjectPath, markdown ?? Markdown);
    }

    public MemoryProjectFileSystem FileSystem { get; }
    public StringWriter Output { get; } = new();

    public CliApplication Application
    {
        get
        {
            var reader = new MarkdownTodoProjectReader();
            var repository = new MarkdownTodoProjectRepository(FileSystem, reader);
            var configuration = new TomlProjectConfigurationLoader(
                "/config.toml",
                candidate => candidate == "/config.toml",
                candidate => candidate == "/config.toml"
                    ? "[projects]\nfiles = [\"/todos/work.md\"]\n"
                    : throw new FileNotFoundException(candidate));
            var service = new TaskImportService(
                configuration,
                new ProjectCatalogLoader(repository),
                new ProjectTodoMutationService(FileSystem, reader));
            var listService = new TaskListService(configuration, new ProjectCatalogLoader(repository));
            return new CliApplication(
                service,
                listService,
                input,
                Output,
                path => throw new FileNotFoundException(path));
        }
    }
}
