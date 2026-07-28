using FluentAssertions;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TextBoxStateTests
{
    [Fact]
    public void ClampedCursor_limits_the_cursor_to_the_text_bounds()
    {
        var beforeText = new TextBoxState("Title", true, "Plan", -2);
        var afterText = new TextBoxState("Title", true, "Plan", 10);

        beforeText.ClampedCursor.Should().Be(0);
        afterText.ClampedCursor.Should().Be(4);
    }

    [Fact]
    public void Edit_exposes_the_configured_editing_state()
    {
        new TextBoxState("Title", true, "Plan", 0).Edit.Should().BeTrue();
        new TextBoxState("Title", false, "Plan", 0).Edit.Should().BeFalse();
    }
}
