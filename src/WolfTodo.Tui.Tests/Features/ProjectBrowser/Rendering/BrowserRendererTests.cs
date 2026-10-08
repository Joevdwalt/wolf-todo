using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser.Rendering;

public sealed class BrowserRendererTests
{
    [Fact]
    public void Todo_formatting_helpers_delegate_to_the_shared_todo_row_renderer()
    {
        var schedule = new TodoSchedule(new DateOnly(2026, 8, 4), new TimeOnly(9, 30));
        var priority = TodoPriority.High;
        var theme = TuiThemes.Wolf;

        BrowserRenderer.FormatSchedule(schedule)
            .Should().Be(TodoRowRenderer.Default.FormatSchedule(schedule));
        BrowserRenderer.FormatDuration(TimeSpan.FromMinutes(45))
            .Should().Be(TodoRowRenderer.Default.FormatDuration(TimeSpan.FromMinutes(45)));
        BrowserRenderer.PriorityCode(priority).Should().Be(TodoRowRenderer.Default.PriorityCode(priority));
        BrowserRenderer.TodoStatusGlyph(true).Should().Be(TodoRowRenderer.Default.StatusGlyph(true));
        BrowserRenderer.PriorityColor(priority, theme).Should().Be(TodoRowRenderer.Default.PriorityColor(priority, theme));
    }

    [Fact]
    public void CreateBrowserRenderContext_calculates_content_height_and_status_lines()
    {
        var today = new DateOnly(2026, 8, 4);
        var renderer = new BrowserRenderer(
            () => 140,
            () => 30,
            () => today);
        var view = new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("All", 0, null, null, true)],
            [],
            null,
            "All",
            null,
            null,
            "No todos");

        var context = renderer.CreateBrowserRenderContext(view, TuiKeyBindings.CreateDefaults(":q"));

        context.Width.Should().Be(140);
        context.Height.Should().Be(30);
        context.Compact.Should().BeFalse();
        context.Today.Should().Be(today);
        context.StatusLines.Should().NotBeEmpty();
        context.ContentHeight.Should().BeGreaterThan(0);
    }
}
