using FluentAssertions;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class SelectOptionTests
{
    [Fact]
    public void Uses_optional_defaults_for_detail_and_enabled_state()
    {
        var option = new SelectOption("Inbox");

        option.Label.Should().Be("Inbox");
        option.Detail.Should().BeNull();
        option.IsEnabled.Should().BeTrue();
    }
}
