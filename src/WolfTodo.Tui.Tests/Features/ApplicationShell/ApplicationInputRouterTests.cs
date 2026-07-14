using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class ApplicationInputRouterTests
{
    private readonly ApplicationInputRouter router = new();
    private readonly TuiKeyBindings bindings = TuiKeyBindings.CreateDefaults(":q");

    [Fact]
    public void Route_returns_tab_directions_for_configured_switch_bindings()
    {
        var next = router.Route(false, Key(shift: false), bindings);
        var previous = router.Route(false, Key(shift: true), bindings);

        next.Should().Be(ApplicationInputRoute.NextTab);
        previous.Should().Be(ApplicationInputRoute.PreviousTab);
    }

    [Fact]
    public void Route_sends_tab_bindings_to_the_feature_while_it_captures_input()
    {
        var result = router.Route(true, Key(shift: false), bindings);

        result.Should().Be(ApplicationInputRoute.ActiveFeature);
    }

    private static ConsoleKeyInfo Key(bool shift) =>
        new('\0', ConsoleKey.Tab, shift, false, true);
}
