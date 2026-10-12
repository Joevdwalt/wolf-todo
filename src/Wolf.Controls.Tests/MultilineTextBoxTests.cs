using FluentAssertions;

namespace Wolf.Controls.Tests;

public sealed class MultilineTextBoxTests
{
    [Fact]
    public void Create_normalizes_line_endings_and_clamps_selection()
    {
        var state = MultilineTextBoxState.Create("Notes", "First\r\nSecond", true);
        var selected = state with { Cursor = 3, SelectionAnchor = 50 };

        state.Text.Should().Be("First\nSecond");
        state.Cursor.Should().Be(state.Text.Length);
        selected.SelectionStart.Should().Be(3);
        selected.SelectionLength.Should().Be(state.Text.Length - 3);
    }

    [Fact]
    public void Reduce_inserts_a_newline_and_accepts_separately()
    {
        var state = MultilineTextBoxState.Create("Notes", "First", true);

        var edited = MultilineTextBox.Default.Reduce(state, new(ControlInputKind.InsertNewline));
        var saved = MultilineTextBox.Default.Reduce(edited.State!, new(ControlInputKind.Accept));

        edited.State!.Text.Should().Be("First\n");
        saved.Outcome.Should().Be(MultilineTextBoxOutcome.Accepted);
        saved.State.Should().Be(edited.State);
    }

    [Fact]
    public void Reduce_replaces_a_selection_with_text_or_newline()
    {
        var state = MultilineTextBoxState.Create("Notes", "First\nSecond", true);
        var selected = MultilineTextBox.Default.Reduce(state, new(ControlInputKind.SelectAll)).State!;

        var typed = MultilineTextBox.Default.Reduce(selected, ControlInput.FromCharacter('R')).State!;
        var newline = MultilineTextBox.Default.Reduce(selected, new(ControlInputKind.InsertNewline)).State!;

        typed.Text.Should().Be("R");
        newline.Text.Should().Be("\n");
        newline.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void Reduce_moves_between_lines_and_cancels_without_a_state()
    {
        var state = new MultilineTextBoxState("Notes", "First\nSecond", 8, true);

        var moved = MultilineTextBox.Default.Reduce(state, new(ControlInputKind.MoveUp)).State!;
        var cancelled = MultilineTextBox.Default.Reduce(state, new(ControlInputKind.Cancel));

        moved.Cursor.Should().Be(2);
        cancelled.Outcome.Should().Be(MultilineTextBoxOutcome.Cancelled);
        cancelled.State.Should().BeNull();
    }

    [Fact]
    public void Component_measures_and_renders_at_boundary_widths()
    {
        var state = new MultilineTextBoxState("Notes", "\nSecond", 0, true);

        MultilineTextBox.Default.Measure(state, new ControlConstraints(0, 0)).Should().Be(4);
        MultilineTextBox.Default.Render(state, ControlTheme.Default, new ControlConstraints(1, 1)).Should().NotBeNull();
        MultilineTextBox.Default.Render(state with { SelectionAnchor = 4 }, ControlTheme.Default, new ControlConstraints(30, 4))
            .Should().NotBeNull();
    }
}
