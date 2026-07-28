using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Core.Tests.Infrastructure.Markdown;

public sealed class MarkdownTodoProjectReaderTests
{
    private readonly MarkdownTodoProjectReader reader = new();

    [Fact]
    public void ParseTitle_uses_the_filename_when_front_matter_is_absent()
    {
        var result = reader.ParseTitle("/todos/client-work.md", ["- [ ] Prepare workshop"]);

        result.Title.Should().Be("client-work");
        result.ContentStart.Should().Be(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ParseTitle_reads_and_trims_a_yaml_title()
    {
        var result = reader.ParseTitle(
            "/todos/client-work.md",
            ["---", "title: '  Client Work  '", "---", "- [ ] Prepare workshop"]);

        result.Title.Should().Be("Client Work");
        result.ContentStart.Should().Be(3);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ParseTitle_uses_the_filename_when_valid_front_matter_has_no_title()
    {
        var result = reader.ParseTitle(
            "/todos/client-work.md",
            ["---", "owner: Joe", "---", "- [ ] Prepare workshop"]);

        result.Title.Should().Be("client-work");
        result.ContentStart.Should().Be(3);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ParseTitle_reports_unclosed_or_invalid_front_matter()
    {
        var unclosed = reader.ParseTitle("/todos/client-work.md", ["---", "title: Client Work"]);
        var invalidTitle = reader.ParseTitle("/todos/client-work.md", ["---", "title: ' '", "---"]);
        var nonStringTitle = reader.ParseTitle("/todos/client-work.md", ["---", "title:", "  - invalid", "---"]);

        unclosed.Error.Should().Be("/todos/client-work.md:1: YAML front matter is not closed.");
        invalidTitle.Error.Should().Be("/todos/client-work.md:1: YAML title must be a non-empty string.");
        nonStringTitle.Error.Should().Be("/todos/client-work.md:1: YAML title must be a non-empty string.");
    }

    [Fact]
    public void ParseHeading_returns_the_level_and_trimmed_title()
    {
        var heading = reader.ParseHeading("###  2026  ###");

        heading.Should().Be(new MarkdownHeading(3, "2026"));
    }

    [Fact]
    public void ParseHeading_returns_null_for_non_headings()
    {
        reader.ParseHeading("- [ ] Prepare workshop").Should().BeNull();
    }

    [Fact]
    public void ParseTodoLine_reads_metadata_and_a_descriptive_reference()
    {
        var result = reader.ParseTodoLine(
            7,
            "  - [ ] (User Story 144734: By Audience) Capture notes ⏫ #now 🛫 2026-07-08 📅 2026-07-31",
            ["Renewals", null, null, null, null, null]);

        result.IsTask.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Line!.Indent.Should().Be(2);
        result.Line.Todo.Should().BeEquivalentTo(new TodoItem(
            7, false, "User Story 144734: By Audience", "Capture notes", TodoPriority.High, ["now"],
            new DateOnly(2026, 7, 8), new DateOnly(2026, 7, 31), "Renewals", [], []));
    }

    [Fact]
    public void ParseTodoLine_treats_legacy_and_unseparated_references_as_title_text()
    {
        var legacy = reader.ParseTodoLine(1, "- [ ] LEGACY-7 - Legacy reference", []);
        var unseparated = reader.ParseTodoLine(2, "- [ ] (ABC-123)Unseparated reference", []);

        legacy.Line!.Todo.Title.Should().Be("LEGACY-7 - Legacy reference");
        legacy.Line.Todo.ExternalReference.Should().BeNull();
        unseparated.Line!.Todo.Title.Should().Be("(ABC-123)Unseparated reference");
        unseparated.Line.Todo.ExternalReference.Should().BeNull();
    }

    [Theory]
    [InlineData("- [ ] Prepare proposal ⏳ 2026-07-15 ⏰ 09:10")]
    [InlineData("- [ ] Prepare proposal ⏳ 2026-07-15 ⏰ 05:30")]
    [InlineData("- [ ] Prepare proposal ⏳ 2026-02-30 ⏰ 09:30")]
    [InlineData("- [ ] Prepare proposal ⏰ 09:10 🔁 every day ⏳ 2026-07-15")]
    public void ParseTodoLine_reports_invalid_schedule_metadata(string line)
    {
        var result = reader.ParseTodoLine(1, line, []);

        result.IsTask.Should().BeTrue();
        result.Error.Should().Contain("schedule must use a valid date");
    }

    [Fact]
    public void ParseTodoLine_reads_schedule_duration_and_unrecognized_metadata()
    {
        var result = reader.ParseTodoLine(
            1,
            "- [ ] Prepare proposal ⏰ 09:15 🔁 every day ⏱ 45m #work ⏳ 2026-07-15",
            []);

        result.Line!.Todo.Title.Should().Be("Prepare proposal 🔁 every day");
        result.Line.Todo.Schedule.Should().Be(new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 15)));
        result.Line.Todo.Duration.Should().Be(TimeSpan.FromMinutes(45));
        result.Line.Todo.Tags.Should().Equal("work");
    }

    [Fact]
    public void ParseTodoLine_preserves_incomplete_metadata_as_title_text_and_reports_duplicate_metadata()
    {
        var incomplete = reader.ParseTodoLine(1, "- [ ] Replace light 📅 tomorrow ⏰ 09:30", []);
        var invalidDate = reader.ParseTodoLine(2, "- [ ] Replace light 📅 2026-02-30", []);
        var duplicate = reader.ParseTodoLine(3, "- [ ] Prepare ⏳ 2026-07-15 ⏳ 2026-07-16", []);

        incomplete.Line!.Todo.Title.Should().Be("Replace light 📅 tomorrow ⏰ 09:30");
        invalidDate.Line!.Todo.Title.Should().Be("Replace light 📅 2026-02-30");
        duplicate.Error.Should().Contain("more than one schedule");
    }

    [Fact]
    public void ParseTodoLine_returns_not_a_task_for_non_task_lines()
    {
        var result = reader.ParseTodoLine(1, "Plain paragraph", []);

        result.IsTask.Should().BeFalse();
        result.Line.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ParseNoteLine_normalizes_bullets_and_preserves_indentation_and_blank_state()
    {
        var note = reader.ParseNoteLine(4, "  - Review current contract");
        var blank = reader.ParseNoteLine(5, "");

        note.Should().Be(new MarkdownNoteLine(4, 2, "Review current contract", false, true));
        blank.Should().Be(new MarkdownNoteLine(5, 0, string.Empty, true, false));
    }

    [Fact]
    public void AddNote_appends_a_note_without_mutating_the_input_todo()
    {
        var child = new TodoItem(2, false, null, "Child", null, [], null, null, string.Empty, [], []);
        var original = new TodoItem(1, false, null, "Parent", null, [], null, null, "Work", [], [child]);
        var line = new MarkdownNoteLine(3, 2, "Review contract", false, true);

        var updated = MarkdownTodoProjectReader.AddNote(original, line, isContinuation: false);

        updated.Notes.Should().Equal(new TodoNote(3, "Review contract"));
        updated.Subtasks.Should().Equal(child);
        original.Notes.Should().BeEmpty();
        original.Subtasks.Should().Equal(child);
        updated.Title.Should().Be(original.Title);
    }

    [Fact]
    public void AddNote_extends_the_previous_note_for_continuations()
    {
        var original = new TodoItem(
            1, false, null, "Parent", null, [], null, null, string.Empty,
            [new TodoNote(2, "First paragraph")], []);

        var textContinuation = MarkdownTodoProjectReader.AddNote(
            original,
            new MarkdownNoteLine(3, 4, "Continues here", false, false),
            isContinuation: true);
        var blankContinuation = MarkdownTodoProjectReader.AddNote(
            textContinuation,
            new MarkdownNoteLine(4, 0, string.Empty, true, false),
            isContinuation: true);

        textContinuation.Notes.Should().Equal(new TodoNote(2, "First paragraph\nContinues here", 2));
        blankContinuation.Notes.Should().Equal(new TodoNote(2, "First paragraph\nContinues here\n", 3));
        original.Notes.Should().Equal(new TodoNote(2, "First paragraph"));
    }

    [Fact]
    public void ParseTodos_builds_heading_paths_subtasks_and_note_continuations()
    {
        string[] lines =
        [
            "## Renewals",
            "- [ ] Parent",
            "  - First paragraph",
            "    Continues here",
            "",
            "    Second paragraph",
            "  - [ ] Child"
        ];

        var result = reader.ParseTodos("/todos/work.md", lines, 0);

        result.IsSuccess.Should().BeTrue();
        var parent = result.Todos.Should().ContainSingle().Subject;
        parent.SectionPath.Should().Be("Renewals");
        parent.Notes.Should().ContainSingle().Which.Text.Should().Be("First paragraph\nContinues here\n\nSecond paragraph");
        parent.Subtasks.Should().ContainSingle().Which.Title.Should().Be("Child");
    }

    [Fact]
    public void ParseTodos_prefixes_task_errors_with_the_source_path_and_line()
    {
        var result = reader.ParseTodos("/todos/work.md", ["- [ ] ⏰ 09:30 ⏳ 2026-07-15"], 0);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("/todos/work.md:1: Todo title must not be empty.");
    }
}
