using System.Text.Json;
using FluentAssertions;
using WolfTodo.Cli;
using WolfTodo.Cli.Features;
using WolfTodo.Cli.Infrastructure;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Cli.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    public void Add_creates_a_full_task_and_returns_machine_readable_result()
    {
        var fixture = new Fixture();

        var exitCode = fixture.Application.Run(
        [
            "add", "--project", "Work", "--title", "Prepare proposal",
            "--reference", "EXT-7", "--priority", "high", "--tag", "#now",
            "--scheduled", "2026-09-01", "--time", "09:30", "--duration-minutes", "30",
            "--note", "Review scope", "--subtask", "Draft", "--completed-subtask", "Brief approved"
        ]);

        exitCode.Should().Be(0);
        using var output = JsonDocument.Parse(fixture.Output.ToString());
        output.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        output.RootElement.GetProperty("created_count").GetInt32().Should().Be(1);
        fixture.FileSystem.Contents.Should().Contain(
            "- [ ] (EXT-7) Prepare proposal ⏰ 09:30 ⏱ 30m ⏫ #now ⏳ 2026-09-01\n" +
            "  - Review scope\n" +
            "  - [ ] Draft\n" +
            "  - [x] Brief approved\n");
    }

    [Fact]
    public void Import_reads_a_strict_json_batch_from_stdin_in_one_write()
    {
        var json = """
            {
              "project": "Work",
              "tasks": [
                { "title": "First", "content": [{ "type": "note", "text": "context" }] },
                { "title": "Second", "priority": "medium", "tags": ["agent"] }
              ]
            }
            """;
        var fixture = new Fixture(json);

        var exitCode = fixture.Application.Run(["import", "--stdin"]);

        exitCode.Should().Be(0);
        fixture.FileSystem.WriteCount.Should().Be(1);
        fixture.FileSystem.Contents.Should().Contain("- [ ] First\n  - context\n- [ ] Second 🔼 #agent\n");
    }

    [Fact]
    public void Import_rejects_unknown_json_properties_without_writing()
    {
        var fixture = new Fixture(markdown: """
            { "project": "Work", "tasks": [{ "title": "Task", "hallucinated": true }] }
            """);

        var exitCode = fixture.Application.Run(["import", "--stdin"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"invalid_json\"");
        fixture.FileSystem.WriteCount.Should().Be(0);
    }

    [Fact]
    public void Add_rejects_missing_required_options_with_exit_code_two()
    {
        var fixture = new Fixture();

        var exitCode = fixture.Application.Run(["add", "--title", "Task"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"missing_option\"");
    }

    [Fact]
    public void Add_rejects_invalid_task_fields_with_exit_code_two()
    {
        var fixture = new Fixture();

        var exitCode = fixture.Application.Run(
            ["add", "--project", "Work", "--title", "Task", "--duration-minutes", "17"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"invalid_task\"");
        fixture.FileSystem.WriteCount.Should().Be(0);
    }

    [Fact]
    public void List_returns_all_configured_tasks_with_their_markdown_metadata()
    {
        var fixture = new Fixture(markdown: """
            ---
            title: Work
            ---

            # Work
            - [ ] (EXT-7) Prepare proposal ⏰ 09:30 ⏱ 30m ⏫ #now ⏳ 2026-09-01
              - Review scope
              - [x] Draft proposal
            """);

        var exitCode = fixture.Application.Run(["list"]);

        exitCode.Should().Be(0);
        using var output = JsonDocument.Parse(fixture.Output.ToString());
        output.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        output.RootElement.GetProperty("task_count").GetInt32().Should().Be(2);
        var task = output.RootElement.GetProperty("tasks")[0];
        task.GetProperty("project").GetProperty("title").GetString().Should().Be("Work");
        task.GetProperty("reference").GetString().Should().Be("EXT-7");
        task.GetProperty("priority").GetString().Should().Be("high");
        task.GetProperty("schedule").GetProperty("date").GetString().Should().Be("2026-09-01");
        task.GetProperty("duration_minutes").GetInt32().Should().Be(30);
        task.GetProperty("notes")[0].GetString().Should().Be("Review scope");
        output.RootElement.GetProperty("tasks")[1].GetProperty("parent_source_line").GetInt32()
            .Should().Be(task.GetProperty("source_line").GetInt32());
    }

    [Fact]
    public void List_rejects_unknown_options_with_exit_code_two()
    {
        var fixture = new Fixture();

        var exitCode = fixture.Application.Run(["list", "--unknown"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"unknown_option\"");
    }

    private sealed class Fixture
    {
        private const string ProjectPath = "/todos/work.md";
        private const string Markdown = "---\ntitle: Work\n---\n\n# Work\n";
        private readonly StringReader input;

        public Fixture(string stdin = "", string? markdown = null)
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
                        : throw new FileNotFoundException());
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

    private sealed class MemoryProjectFileSystem(string path, string contents) : IProjectFileSystem
    {
        public string Contents { get; private set; } = contents;
        public int WriteCount { get; private set; }

        public bool FileExists(string candidate) => candidate == path;
        public string GetFullPath(string candidate) => candidate;
        public string ReadAllText(string candidate) => candidate == path
            ? Contents
            : throw new FileNotFoundException(candidate);

        public void WriteAllTextAtomically(string candidate, string value)
        {
            candidate.Should().Be(path);
            Contents = value;
            WriteCount++;
        }
    }
}
