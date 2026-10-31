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
    public void Details_render_identity_metadata_and_omit_missing_fields()
    {
        var todo = Todo("Prepare") with
        {
            ExternalReference = "REF-1", Tags = ["work"], Schedule = new TodoSchedule(new DateOnly(2026, 8, 4), null),
            Duration = TimeSpan.FromMinutes(30)
        };
        var lines = new List<IRenderable>();
        renderer.AddTitle(lines, todo, TuiThemes.Wolf);
        renderer.AddIdentity(lines, "Work", new TodoIdentity("/work.md", 1), TuiThemes.Wolf);
        renderer.AddMetadata(lines, todo, TuiThemes.Wolf);
        var text = Render(lines);
        text.Should().Contain("Prepare").And.Contain("PROJECT: Work").And.Contain("LINK:")
            .And.Contain("REFERENCE: REF-1").And.Contain("TAGS: #work")
            .And.Contain("SCHEDULED: 2026-08-04").And.Contain("DURATION: 30m")
            .And.NotContain("PRIORITY:").And.NotContain("SECTION:");
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
