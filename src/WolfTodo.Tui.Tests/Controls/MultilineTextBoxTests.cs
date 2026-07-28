using FluentAssertions;
using Spectre.Console;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class MultilineTextBoxTests
{
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");

    [Fact]
    public void Reduce_inserts_new_lines_and_emits_a_save_outcome()
    {
        var state = MultilineTextBoxState.Create("Notes", "First", isMultiline: true);

        var editing = MultilineTextBox.Default.Reduce(state, Key(ConsoleKey.Enter), Bindings);
        var accepted = MultilineTextBox.Default.Reduce(editing.State!, Key(ConsoleKey.S, control: true), Bindings);

        editing.State!.Text.Should().Be("First\n");
        accepted.Outcome.Should().Be(MultilineTextBoxOutcome.Accepted);
        accepted.State.Should().Be(editing.State);
    }

    [Fact]
    public void Reduce_cancels_without_returning_a_state()
    {
        var state = MultilineTextBoxState.Create("Notes", "First", isMultiline: true);

        var transition = MultilineTextBox.Default.Reduce(state, Key(ConsoleKey.Escape), Bindings);

        transition.Outcome.Should().Be(MultilineTextBoxOutcome.Cancelled);
        transition.State.Should().BeNull();
    }

    [Fact]
    public void Component_measures_and_renders_with_the_supplied_row_constraint()
    {
        var state = MultilineTextBoxState.Create("Notes", "First\nSecond", isMultiline: true);
        ITuiComponent<MultilineTextBoxState, MultilineTextBoxOutcome> component = MultilineTextBox.Default;

        component.Measure(state, new TuiComponentConstraints(30, 4)).Should().Be(7);
        component.Render(state, TuiThemes.Wolf, new TuiComponentConstraints(30, 4)).Should().NotBeNull();
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) => new('\0', key, false, false, control);
}
