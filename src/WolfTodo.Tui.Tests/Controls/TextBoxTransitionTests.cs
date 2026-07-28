using FluentAssertions;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TextBoxTransitionTests
{
    [Fact]
    public void Creates_an_editing_transition_by_default()
    {
        var state = TextBox.Create("Title", true, "Plan");

        var transition = new TextBoxTransition(state);

        transition.State.Should().Be(state);
        transition.Outcome.Should().Be(TextBoxOutcome.Editing);
    }
}
