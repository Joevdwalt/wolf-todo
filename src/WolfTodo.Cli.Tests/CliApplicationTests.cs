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
        fixture.Output.ToString().Should().Contain("wtodo update <task-code>");
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
        foreach (var entry in output.RootElement.GetProperty("tasks").EnumerateArray())
        {
            var code = entry.GetProperty("task_code").GetString()!;
            TaskLinkCode.IsValid(code).Should().BeTrue();
            code.Should().Be(TaskLinkCode.Generate(
                entry.GetProperty("project").GetProperty("path").GetString()!,
                entry.GetProperty("source_line").GetInt32()));
        }
        output.RootElement.GetProperty("tasks")[1].GetProperty("task_code").GetString()
            .Should().NotBe(task.GetProperty("task_code").GetString());
    }

    [Fact]
    public void List_rejects_unknown_options_with_exit_code_two()
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(["list", "--unknown"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"unknown_option\"");
    }

    [Fact]
    public void Get_returns_the_exact_nested_completed_task_in_the_list_shape()
    {
        var fixture = new CliApplicationFixture(markdown: """
            ---
            title: Work
            ---

            # Work
            - [ ] Parent
              - [x] Child
            """);
        var code = TaskLinkCode.Generate("/todos/work.md", 7);

        var exitCode = fixture.Application.Run(["get", code]);

        exitCode.Should().Be(0);
        using var output = JsonDocument.Parse(fixture.Output.ToString());
        var task = output.RootElement.GetProperty("task");
        task.GetProperty("task_code").GetString().Should().Be(code);
        task.GetProperty("title").GetString().Should().Be("Child");
        task.GetProperty("completed").GetBoolean().Should().BeTrue();
        task.GetProperty("parent_source_line").GetInt32().Should().Be(6);
        task.GetProperty("project").GetProperty("title").GetString().Should().Be("Work");
    }

    [Theory]
    [InlineData("invalid", "invalid_task_code", 2)]
    [InlineData("wt1-00000000", "task_not_found", 1)]
    public void Get_reports_lookup_failures_with_documented_codes(string code, string errorCode, int expectedExitCode)
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(["get", code]);

        exitCode.Should().Be(expectedExitCode);
        using var output = JsonDocument.Parse(fixture.Output.ToString());
        output.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
        output.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be(errorCode);
    }

    [Fact]
    public void Get_requires_a_task_code_argument()
    {
        var fixture = new CliApplicationFixture();

        var exitCode = fixture.Application.Run(["get"]);

        exitCode.Should().Be(2);
        fixture.Output.ToString().Should().Contain("\"code\":\"missing_argument\"");
    }

    [Fact]
    public void Update_changes_a_task_by_link_code_and_preserves_omitted_fields()
    {
        var fixture = new CliApplicationFixture(markdown: """
            ---
            title: Work
            ---

            # Work
            - [ ] (EXT-7) Parent ⏫ #old ⏳ 2026-09-01
              - existing note
              - [ ] Child
            """);
        var code = TaskLinkCode.Generate(
            "/todos/work.md",
            new MarkdownTodoProjectReader().Parse("/todos/work.md", fixture.FileSystem.Contents)
                .Project!.Todos.Single().SourceLine);

        var exitCode = fixture.Application.Run([
            "update", code, "--completed", "true", "--title", "Renamed",
            "--tag", "new", "--content", "updated note"]);

        exitCode.Should().Be(0);
        fixture.FileSystem.WriteCount.Should().Be(1);
        fixture.FileSystem.Contents.Should().Contain(
            "- [x] (EXT-7) Renamed ⏫ #new ⏳ 2026-09-01\n" +
            "  - updated note\n" +
            "  - [ ] Child");
        using var output = JsonDocument.Parse(fixture.Output.ToString());
        var task = output.RootElement.GetProperty("task");
        task.GetProperty("task_code").GetString().Should().Be(code);
        task.GetProperty("completed").GetBoolean().Should().BeTrue();
        task.GetProperty("reference").GetString().Should().Be("EXT-7");
        task.GetProperty("priority").GetString().Should().Be("high");
    }

    [Fact]
    public void Update_changes_a_nested_task_and_supports_explicit_clears()
    {
        var fixture = new CliApplicationFixture(markdown: """
            ---
            title: Work
            ---

            # Work
            - [ ] Parent
              - [x] (EXT-7) Child ⏰ 09:30 ⏱ 30m #old ⏳ 2026-09-01
                - note
            """);
        var code = TaskLinkCode.Generate(
            "/todos/work.md",
            new MarkdownTodoProjectReader().Parse("/todos/work.md", fixture.FileSystem.Contents)
                .Project!.Todos.Single().Subtasks.Single().SourceLine);

        var exitCode = fixture.Application.Run([
            "update", code, "--completed", "false", "--clear-reference", "--clear-priority",
            "--clear-tags", "--clear-schedule", "--clear-duration", "--clear-content"]);

        exitCode.Should().Be(0);
        fixture.FileSystem.Contents.Should().Contain("  - [ ] Child");
        fixture.FileSystem.Contents.Should().NotContain("EXT-7");
        fixture.FileSystem.Contents.Should().NotContain("⏰");
        fixture.FileSystem.Contents.Should().NotContain("⏱");
        fixture.FileSystem.Contents.Should().NotContain("#old");
        fixture.FileSystem.Contents.Should().NotContain("    - note");
        using var output = JsonDocument.Parse(fixture.Output.ToString());
        output.RootElement.GetProperty("task").GetProperty("parent_source_line").GetInt32().Should().Be(6);
    }

    [Fact]
    public void Update_rejects_missing_changes_and_conflicting_options_without_writing()
    {
        var fixture = new CliApplicationFixture(markdown: "- [ ] Task\n");
        var code = TaskLinkCode.Generate("/todos/work.md", 1);

        var missing = fixture.Application.Run(["update", code]);
        missing.Should().Be(2);
        fixture.FileSystem.WriteCount.Should().Be(0);
        fixture.Output.ToString().Should().Contain("\"code\":\"missing_update\"");

        var conflicting = fixture.Application.Run([
            "update", code, "--reference", "EXT-1", "--clear-reference"]);
        conflicting.Should().Be(2);
        fixture.FileSystem.WriteCount.Should().Be(0);
        fixture.Output.ToString().Should().Contain("\"code\":\"conflicting_options\"");
    }

    [Fact]
    public void Update_rejects_invalid_task_fields_without_writing()
    {
        var fixture = new CliApplicationFixture(markdown: "- [ ] Task\n");
        var code = TaskLinkCode.Generate("/todos/work.md", 1);

        var exitCode = fixture.Application.Run(["update", code, "--title", " "]);

        exitCode.Should().Be(2);
        fixture.FileSystem.WriteCount.Should().Be(0);
        fixture.Output.ToString().Should().Contain("\"code\":\"invalid_task\"");
    }

    [Fact]
    public void Update_rejects_a_timed_schedule_that_collides_with_another_task()
    {
        var fixture = new CliApplicationFixture(markdown: """
            ---
            title: Work
            ---

            # Work
            - [ ] First ⏰ 09:00 ⏳ 2026-09-01
            - [ ] Second ⏰ 10:00 ⏳ 2026-09-01
            """);
        var code = TaskLinkCode.Generate("/todos/work.md", 7);

        var exitCode = fixture.Application.Run([
            "update", code, "--time", "09:00"]);

        exitCode.Should().Be(1);
        fixture.FileSystem.WriteCount.Should().Be(0);
        fixture.Output.ToString().Should().Contain("\"code\":\"schedule_conflict\"");
    }

}
