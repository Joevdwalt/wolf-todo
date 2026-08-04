using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class BrowserRendererTests
{
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
