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
    [Theory]
    [InlineData("help")]
    [InlineData("-h")]
    [InlineData("--help")]
    public void Help_aliases_write_the_legacy_help_text(string help)
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run([help]);

        exitCode.Should().Be(0);
        fixture.Output.ToString().Should().Contain("Wolf Todo CLI");
        fixture.Output.ToString().Should().Contain("wtodo import --stdin");
    }

    [Fact]
    public void Unknown_command_writes_one_structured_error()
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(["unknown"]);

        exitCode.Should().Be(2);
        using var output = JsonDocument.Parse(fixture.Output.ToString());
        output.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("unknown_command");
    }

    [Fact]
    public void Missing_option_value_keeps_the_structured_error_code()
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(["list", "--project"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"missing_value\"");
    }

    [Fact]
    public void Add_writes_content_before_separate_subtasks()
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(
        [
            "add", "--project", "Work", "--title", "Task",
            "--subtask", "First", "--content", "Context", "--completed-subtask", "Done"
        ]);

        exitCode.Should().Be(0);
        fixture.FileSystem.Contents.Should().Contain(
            "- [ ] Task\n" +
            "  - Context\n" +
            "  - [ ] First\n" +
            "  - [x] Done\n");
    }

    [Fact]
    public void Add_creates_a_full_task_and_returns_machine_readable_result()
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(
        [
            "add", "--project", "Work", "--title", "Prepare proposal",
            "--reference", "EXT-7", "--priority", "high", "--tag", "#now",
            "--scheduled", "2026-09-01", "--time", "09:30", "--duration-minutes", "30",
            "--content", "Review scope", "--subtask", "Draft", "--completed-subtask", "Brief approved"
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
                { "title": "First", "content": "context" },
                { "title": "Second", "priority": "medium", "tags": ["agent"] }
              ]
            }
            """;
        var fixture = new CliApplicationFixture(json);

        var exitCode = fixture.Application.Run(["import", "--stdin"]);

        exitCode.Should().Be(0);
        fixture.FileSystem.WriteCount.Should().Be(1);
        fixture.FileSystem.Contents.Should().Contain("- [ ] First\n  - context\n- [ ] Second 🔼 #agent\n");
    }

    [Fact]
    public void Import_rejects_unknown_json_properties_without_writing()
    {
        var fixture = new CliApplicationFixture(stdin: """
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
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(["add", "--title", "Task"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"missing_option\"");
    }

    [Fact]
    public void Add_rejects_duplicate_scalar_options()
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(
            ["add", "--project", "Work", "--project", "Other", "--title", "Task"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"duplicate_option\"");
        fixture.FileSystem.WriteCount.Should().Be(0);
    }

    [Fact]
    public void Import_rejects_duplicate_stdin_options()
    {
        var fixture = new CliApplicationFixture("{}");

        var exitCode = fixture.Application.Run(["import", "--stdin", "--stdin"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"duplicate_option\"");
    }

    [Fact]
    public void Add_rejects_invalid_task_fields_with_exit_code_two()
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(
            ["add", "--project", "Work", "--title", "Task", "--duration-minutes", "17"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"invalid_task\"");
        fixture.FileSystem.WriteCount.Should().Be(0);
    }

    [Fact]
    public void List_returns_all_configured_tasks_with_their_markdown_metadata()
    {
        var fixture = new CliApplicationFixture(markdown: """
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
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(["list", "--unknown"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"unknown_option\"");
    }

}
