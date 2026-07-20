using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Tests.Features.ProjectBrowser;

public sealed class ProjectMarkdownParserTests
{
    private readonly ProjectMarkdownParser parser = new();

    [Fact]
    public void Parse_reads_project_metadata_todo_fields_notes_and_subtasks()
    {
        const string markdown = """
            ---
            title: Client Contracts
            ---

            ## Renewals

            - [ ] 134416 - Milas Contract Renewal ⏫ #now 🛫 2026-07-08 📅 2026-07-31
              - Review current contract
              - Update proposal costing
              - [ ] Confirm outstanding issues
            """;

        var result = parser.Parse("/todos/contracts.md", markdown);

        result.IsSuccess.Should().BeTrue();
        result.Project!.Title.Should().Be("Client Contracts");
        var todo = result.Project.Todos.Should().ContainSingle().Subject;
        todo.ExternalReference.Should().Be("134416");
        todo.Title.Should().Be("Milas Contract Renewal");
        todo.Priority.Should().Be(TodoPriority.High);
        todo.Tags.Should().Equal("now");
        todo.StartDate.Should().Be(new DateOnly(2026, 7, 8));
        todo.DueDate.Should().Be(new DateOnly(2026, 7, 31));
        todo.SectionPath.Should().Be("Renewals");
        todo.Notes.Select(note => note.Text)
            .Should().Equal("Review current contract", "Update proposal costing");
        todo.Notes.Select(note => note.SourceLine).Should().Equal(8, 9);
        todo.Subtasks.Should().ContainSingle().Which.Title.Should().Be("Confirm outstanding issues");
    }

    [Fact]
    public void Parse_uses_filename_when_front_matter_title_is_absent()
    {
        var result = parser.Parse("/todos/home.md", "- [ ] Replace light");

        result.Project!.Title.Should().Be("home");
    }

    [Fact]
    public void Parse_preserves_an_invalid_calendar_date_as_title_text()
    {
        var result = parser.Parse("/todos/home.md", "- [ ] Replace light 📅 2026-02-30");

        result.IsSuccess.Should().BeTrue();
        result.Project!.Todos.Single().Title.Should().Be("Replace light 📅 2026-02-30");
        result.Project.Todos.Single().DueDate.Should().BeNull();
    }

    [Fact]
    public void Parse_preserves_a_date_marker_without_iso_date_as_title_text()
    {
        var result = parser.Parse("/todos/home.md", "- [ ] Replace light 📅 tomorrow");

        result.IsSuccess.Should().BeTrue();
        result.Project!.Todos.Single().Title.Should().Be("Replace light 📅 tomorrow");
        result.Project.Todos.Single().DueDate.Should().BeNull();
    }

    [Fact]
    public void Parse_reads_the_legacy_schedule_order_for_compatibility()
    {
        var result = parser.Parse(
            "/todos/home.md",
            "- [ ] Prepare proposal ⏳ 2026-07-15 ⏰ 09:30 #work");

        var todo = result.Project!.Todos.Single();
        todo.Title.Should().Be("Prepare proposal");
        todo.Schedule.Should().Be(new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 30)));
        todo.Tags.Should().Equal("work");
    }

    [Fact]
    public void Parse_reads_clock_first_schedule_with_task_markers_between_the_tokens()
    {
        var result = parser.Parse(
            "/todos/home.md",
            "- [ ] Prepare proposal ⏰ 09:30 🔁 every day ⏫ #work ⏳ 2026-07-15");

        var todo = result.Project!.Todos.Single();
        todo.Title.Should().Be("Prepare proposal 🔁 every day");
        todo.Schedule.Should().Be(new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 30)));
        todo.Priority.Should().Be(TodoPriority.High);
        todo.Tags.Should().Equal("work");
    }

    [Theory]
    [InlineData("⏳ 2026-07-15 ⏰ 09:15")]
    [InlineData("⏳ 2026-07-15 ⏰ 05:30")]
    [InlineData("⏳ 2026-02-30 ⏰ 09:30")]
    [InlineData("⏰ 09:15 🔁 every day ⏳ 2026-07-15")]
    public void Parse_rejects_complete_but_invalid_schedule_metadata(string schedule)
    {
        var result = parser.Parse("/todos/home.md", $"- [ ] Prepare proposal {schedule}");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("schedule must use a valid date");
    }

    [Fact]
    public void Parse_reads_a_standalone_scheduled_date_as_an_all_day_schedule()
    {
        var result = parser.Parse("/todos/home.md", "- [ ] Prepare proposal ⏳ 2026-07-15");

        result.Project!.Todos.Single().Title.Should().Be("Prepare proposal");
        result.Project.Todos.Single().Schedule.Should().Be(new TodoSchedule(new DateOnly(2026, 7, 15)));
    }

    [Fact]
    public void Parse_preserves_a_standalone_wolf_time_as_title_text()
    {
        var result = parser.Parse("/todos/home.md", "- [ ] Prepare proposal ⏰ 09:30");

        result.Project!.Todos.Single().Title.Should().Be("Prepare proposal ⏰ 09:30");
        result.Project.Todos.Single().Schedule.Should().BeNull();
    }

    [Fact]
    public void Parse_rejects_duplicate_clock_or_scheduled_date_tokens()
    {
        var duplicateClock = parser.Parse(
            "/todos/home.md",
            "- [ ] Prepare proposal ⏰ 09:00 ⏰ 09:30 ⏳ 2026-07-15");
        var duplicateDate = parser.Parse(
            "/todos/home.md",
            "- [ ] Prepare proposal ⏰ 09:30 ⏳ 2026-07-15 ⏳ 2026-07-16");

        duplicateClock.Error.Should().Contain("more than one schedule");
        duplicateDate.Error.Should().Contain("more than one schedule");
    }

    [Fact]
    public void Parse_reports_invalid_yaml_title()
    {
        const string markdown = """
            ---
            title:
              - invalid
            ---
            - [ ] Replace light
            """;

        var result = parser.Parse("/todos/home.md", markdown);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("title must be a non-empty string");
    }
}
