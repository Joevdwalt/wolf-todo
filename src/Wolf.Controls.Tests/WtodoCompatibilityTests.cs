using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;

namespace Wolf.Controls.Tests;

public sealed class WtodoCompatibilityTests
{
    [Fact]
    public void Adapter_maps_wtodo_theme_to_the_independent_control_theme()
    {
        var theme = WtodoControlAdapter.ToControlTheme(TuiThemes.Wolf);

        theme.Accent.Should().Be(TuiThemes.Wolf.Accent);
        theme.Surface.Should().Be(TuiThemes.Wolf.Surface);
        theme.Error.Should().Be(TuiThemes.Wolf.Error);
    }

    [Fact]
    public void Adapter_maps_configured_wtodo_navigation_and_terminal_input_to_semantic_input()
    {
        var bindings = TuiKeyBindings.CreateDefaults(":q");

        WtodoControlAdapter.ToControlInput(new ConsoleKeyInfo('j', ConsoleKey.J, false, false, false), bindings).Kind
            .Should().Be(ControlInputKind.MoveDown);
        WtodoControlAdapter.ToControlInput(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false), bindings)
            .Should().Be(ControlInput.FromCharacter('x'));
    }
}
