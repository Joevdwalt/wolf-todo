using FluentAssertions;
using WolfTodo.Cli.Features;
using WolfTodo.Cli.Infrastructure;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Cli.Tests.Features;

public sealed class TaskImportServiceTests
{
    [Fact]
    public void Import_rejects_a_schedule_occupied_by_a_configured_task()
    {
        const string path = "/todos/work.md";
        var fileSystem = new MemoryFileSystem(path, "---\ntitle: Work\n---\n- [ ] Existing ⏰ 09:30 ⏳ 2026-09-01\n");
        var service = CreateService(fileSystem, path);
        var task = new TodoTaskUpdate(
            new TodoUpdate(
                "Collision", null, null, [], null, null,
                new TodoSchedule(new DateOnly(2026, 9, 1), new TimeOnly(9, 30))),
            new TodoContentUpdate([]));

        var result = service.Import("Work", [task]);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("schedule_conflict");
        fileSystem.WriteCount.Should().Be(0);
    }

    [Fact]
    public void Import_rejects_an_unconfigured_absolute_path()
    {
        const string path = "/todos/work.md";
        var fileSystem = new MemoryFileSystem(path, "# Work\n");

        var result = CreateService(fileSystem, path).Import(
            "/todos/other.md",
            [new TodoTaskUpdate(new TodoUpdate("Task", null, null, [], null, null), new TodoContentUpdate([]))]);

        result.ErrorCode.Should().Be("project_not_configured");
    }

    [Fact]
    public void Import_rejects_duplicate_timed_schedules_inside_the_batch()
    {
        const string path = "/todos/work.md";
        var fileSystem = new MemoryFileSystem(path, "---\ntitle: Work\n---\n# Work\n");
        var schedule = new TodoSchedule(new DateOnly(2026, 9, 1), new TimeOnly(9, 30));
        TodoTaskUpdate Task(string title) => new(
            new TodoUpdate(title, null, null, [], null, null, schedule),
            new TodoContentUpdate([]));

        var result = CreateService(fileSystem, path).Import("Work", [Task("First"), Task("Second")]);

        result.ErrorCode.Should().Be("schedule_conflict");
        fileSystem.WriteCount.Should().Be(0);
    }

    [Fact]
    public void Import_rejects_an_ambiguous_project_title()
    {
        const string firstPath = "/todos/work.md";
        const string secondPath = "/todos/other.md";
        const string markdown = "---\ntitle: Work\n---\n# Work\n";
        var fileSystem = new MultiFileSystem(new Dictionary<string, string>
        {
            [firstPath] = markdown,
            [secondPath] = markdown
        });
        var reader = new MarkdownTodoProjectReader();
        var service = new TaskImportService(
            new TomlProjectConfigurationLoader(
                "/config.toml",
                candidate => true,
                candidate => $"[projects]\nfiles = [\"{firstPath}\", \"{secondPath}\"]\n"),
            new ProjectCatalogLoader(new MarkdownTodoProjectRepository(fileSystem, reader)),
            new ProjectTodoMutationService(fileSystem, reader));

        var result = service.Import(
            "Work",
            [new TodoTaskUpdate(new TodoUpdate("Task", null, null, [], null, null), new TodoContentUpdate([]))]);

        result.ErrorCode.Should().Be("ambiguous_project");
    }

    private static TaskImportService CreateService(MemoryFileSystem fileSystem, string path)
    {
        var reader = new MarkdownTodoProjectReader();
        return new TaskImportService(
            new TomlProjectConfigurationLoader(
                "/config.toml",
                candidate => candidate == "/config.toml",
                candidate => $"[projects]\nfiles = [\"{path}\"]\n"),
            new ProjectCatalogLoader(new MarkdownTodoProjectRepository(fileSystem, reader)),
            new ProjectTodoMutationService(fileSystem, reader));
    }

}
