using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TuiControlPanelTests
{
    [Fact]
    public void Create_returns_a_renderable_panel_for_the_supplied_title_content_and_theme()
    {
        IRenderable content = new Text("Choose a project");

        var panel = TuiControlPanel.Create("Projects", content, TuiThemes.Wolf);

        panel.Should().NotBeNull();
        panel.Should().BeAssignableTo<IRenderable>();
    }
}
