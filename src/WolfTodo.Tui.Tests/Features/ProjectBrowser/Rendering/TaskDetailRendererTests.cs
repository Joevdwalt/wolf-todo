using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser.Rendering;

public sealed class TaskDetailRendererTests
{
    private readonly TaskDetailRenderer renderer = new();

    [Fact]
    public void AddTitle_preserves_literal_title_text()
    {
        var lines = new List<IRenderable>();
        renderer.AddTitle(lines, Todo("Prepare [draft]"), TuiThemes.Wolf);
        Render(lines).TrimEnd().Should().Be("Prepare [draft]");
    }

    [Fact]
    public void AddIdentity_shows_project_and_exact_task_link()
    {
        var lines = new List<IRenderable>();
        renderer.AddIdentity(lines, "Work", new TodoIdentity("/work.md", 1), TuiThemes.Wolf);
        Render(lines).Should().Contain("PROJECT: Work")
            .And.Contain("LINK: " + TaskLinkCode.Generate("/work.md", 1));
    }

    [Fact]
    public void AddIdentity_omits_missing_project_and_identity()
    {
        var lines = new List<IRenderable>();
        renderer.AddIdentity(lines, null, null, TuiThemes.Wolf);
        lines.Should().BeEmpty();
    }

    [Fact]
    public void AddMetadata_shows_task_fields_and_excludes_start_and_due_dates()
    {
        var todo = Todo("Prepare") with
        {
            ExternalReference = "REF-1", Priority = TodoPriority.High, Tags = ["work", "now"], SectionPath = "Renewals",
            StartDate = new DateOnly(2026, 7, 8), DueDate = new DateOnly(2026, 7, 12),
            Schedule = new TodoSchedule(new DateOnly(2026, 8, 4), new TimeOnly(9, 30)), Duration = TimeSpan.FromMinutes(30)
        };
        var lines = new List<IRenderable>();
        renderer.AddMetadata(lines, todo, TuiThemes.Wolf);
        Render(lines).Should().Contain("REFERENCE: REF-1").And.Contain("PRIORITY: High")
            .And.Contain("SECTION: Renewals").And.Contain("TAGS: #work, #now")
            .And.Contain("SCHEDULED: 2026-08-04 09:30").And.Contain("DURATION: 30m")
            .And.NotContain("2026-07-08").And.NotContain("2026-07-12");
    }

    [Fact]
    public void AddMetadata_omits_missing_optional_fields()
    {
        var lines = new List<IRenderable>();
        renderer.AddMetadata(lines, Todo("Empty"), TuiThemes.Wolf);
        lines.Should().BeEmpty();
    }

    [Fact]
    public void AddMetadata_can_omit_section_and_schedule_for_all_day_composition()
    {
        var todo = Todo("Prepare") with
        {
            SectionPath = "Renewals", Schedule = new TodoSchedule(new DateOnly(2026, 8, 4)), ExternalReference = "REF-1"
        };
        var lines = new List<IRenderable>();
        renderer.AddMetadata(lines, todo, TuiThemes.Wolf, includeSection: false, includeSchedule: false);
        Render(lines).TrimEnd().Should().Be("REFERENCE: REF-1");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Content_preserves_each_views_subtask_depth(bool recursive)
    {
        var todo = Todo("Parent") with
        {
            Notes = [new TodoNote(2, "A note")],
            Subtasks = [Todo("Child") with { Subtasks = [Todo("Grandchild")] }]
        };
        var lines = new List<IRenderable>();
        renderer.AddContent(lines, todo, TuiThemes.Wolf, recursive, dimEmpty: false);
        var text = Render(lines);
        text.Should().Contain("NOTES").And.Contain("A note").And.Contain("SUBTASKS").And.Contain("Child");
        if (recursive) text.Should().Contain("Grandchild");
        else text.Should().NotContain("Grandchild");
    }

    [Fact]
    public void Empty_content_displays_placeholder()
    {
        var lines = new List<IRenderable>();
        renderer.AddContent(lines, Todo("Empty"), TuiThemes.Wolf, false, true);
        Render(lines).Should().Contain("NO ADDITIONAL DETAILS");
    }

    private static TodoItem Todo(string title) => new(1, false, null, title, null, [], null, null, string.Empty, [], []);

    private static string Render(IEnumerable<IRenderable> lines)
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Ansi = AnsiSupport.No, Out = new AnsiConsoleOutput(writer) });
        console.Profile.Width = 140;
        console.Write(new Rows(lines));
        return writer.ToString();
    }
}
