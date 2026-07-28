using FluentAssertions;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TuiComponentTransitionTests
{
    [Fact]
    public void Preserves_the_next_state_and_semantic_outcome()
    {
        var state = TextBox.Create("Title", true, "Plan");

        var transition = new TuiComponentTransition<TextBoxState, TextBoxOutcome>(state, TextBoxOutcome.Accepted);

        transition.State.Should().Be(state);
        transition.Outcome.Should().Be(TextBoxOutcome.Accepted);
    }
}
