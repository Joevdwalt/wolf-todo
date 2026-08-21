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
    public void Reduce_selects_all_and_printable_input_replaces_the_selection()
    {
        var state = MultilineTextBoxState.Create("Notes", "First\nSecond", isMultiline: true);

        var selected = MultilineTextBox.Default.Reduce(state, Key(ConsoleKey.A, control: true), Bindings).State!;
        var replaced = MultilineTextBox.Default.Reduce(selected, Key('R'), Bindings).State!;

        selected.HasSelection.Should().BeTrue();
        selected.SelectionStart.Should().Be(0);
        selected.SelectionLength.Should().Be(state.Text.Length);
        replaced.Text.Should().Be("R");
        replaced.Cursor.Should().Be(1);
        replaced.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void Reduce_replaces_the_selection_with_a_newline_in_multiline_mode()
    {
        var state = MultilineTextBoxState.Create("Notes", "First\nSecond", isMultiline: true);
        state = MultilineTextBox.Default.Reduce(state, Key(ConsoleKey.A, control: true), Bindings).State!;

        var replaced = MultilineTextBox.Default.Reduce(state, Key(ConsoleKey.Enter), Bindings).State!;

        replaced.Text.Should().Be("\n");
        replaced.Cursor.Should().Be(1);
        replaced.HasSelection.Should().BeFalse();
    }

    [Theory]
    [InlineData(ConsoleKey.Backspace)]
    [InlineData(ConsoleKey.Delete)]
    public void Reduce_deletes_the_complete_selection(ConsoleKey key)
    {
        var state = MultilineTextBoxState.Create("Notes", "First\nSecond", isMultiline: true);
        state = MultilineTextBox.Default.Reduce(state, Key(ConsoleKey.A, control: true), Bindings).State!;

        var deleted = MultilineTextBox.Default.Reduce(state, Key(key), Bindings).State!;

        deleted.Text.Should().BeEmpty();
        deleted.Cursor.Should().Be(0);
        deleted.HasSelection.Should().BeFalse();
    }

    [Theory]
    [InlineData(ConsoleKey.LeftArrow, 0)]
    [InlineData(ConsoleKey.UpArrow, 0)]
    [InlineData(ConsoleKey.Home, 0)]
    [InlineData(ConsoleKey.RightArrow, 12)]
    [InlineData(ConsoleKey.DownArrow, 12)]
    [InlineData(ConsoleKey.End, 12)]
    public void Reduce_collapses_the_selection_in_the_navigation_direction(ConsoleKey key, int expectedCursor)
    {
        var state = MultilineTextBoxState.Create("Notes", "First\nSecond", isMultiline: true);
        state = MultilineTextBox.Default.Reduce(state, Key(ConsoleKey.A, control: true), Bindings).State!;

        var moved = MultilineTextBox.Default.Reduce(state, Key(key), Bindings).State!;

        moved.Cursor.Should().Be(expectedCursor);
        moved.HasSelection.Should().BeFalse();
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

        var selected = component.Reduce(state, Key(ConsoleKey.A, control: true), Bindings).State!;
        component.Render(selected, TuiThemes.Wolf, new TuiComponentConstraints(30, 4)).Should().NotBeNull();
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) => new('\0', key, false, false, control);

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem2, false, false, false);
}
