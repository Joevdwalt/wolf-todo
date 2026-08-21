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

    [Fact]
    public void Selection_properties_clamp_and_normalize_the_anchor_and_cursor()
    {
        var forward = new TextBoxState("Title", true, "Plan", 4, SelectionAnchor: -2);
        var backward = new TextBoxState("Title", true, "Plan", 1, SelectionAnchor: 10);
        var collapsed = new TextBoxState("Title", true, "Plan", 2, SelectionAnchor: 2);

        forward.HasSelection.Should().BeTrue();
        forward.SelectionStart.Should().Be(0);
        forward.SelectionLength.Should().Be(4);
        backward.HasSelection.Should().BeTrue();
        backward.SelectionStart.Should().Be(1);
        backward.SelectionLength.Should().Be(3);
        collapsed.HasSelection.Should().BeFalse();
        collapsed.SelectionLength.Should().Be(0);
    }
}
