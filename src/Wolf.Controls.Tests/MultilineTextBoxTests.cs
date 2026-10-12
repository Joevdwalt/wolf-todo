using FluentAssertions;
using Spectre.Console;
using Wolf.Controls.MultilineTextBox;

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
    public void State_clamps_cursor_and_reports_empty_selection()
    {
        var before = new MultilineTextBoxState("Notes", "Text", -2, true);
        var after = new MultilineTextBoxState("Notes", "Text", 9, true, SelectionAnchor: 9);

        before.ClampedCursor.Should().Be(0);
        after.ClampedCursor.Should().Be(4);
        after.ClampedSelectionAnchor.Should().Be(4);
        after.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void Reduce_inserts_a_newline_and_accepts_separately()
    {
        var state = MultilineTextBoxState.Create("Notes", "First", true);

        var edited = MultilineTextBox.MultilineTextBox.Default.Reduce(state, new(ControlInputKind.InsertNewline));
        var saved = MultilineTextBox.MultilineTextBox.Default.Reduce(edited.State!, new(ControlInputKind.Accept));

        edited.State!.Text.Should().Be("First\n");
        saved.Outcome.Should().Be(MultilineTextBoxOutcome.Accepted);
        saved.State.Should().Be(edited.State);
    }

    [Fact]
    public void Reduce_replaces_a_selection_with_text_or_newline()
    {
        var state = MultilineTextBoxState.Create("Notes", "First\nSecond", true);
        var selected = MultilineTextBox.MultilineTextBox.Default.Reduce(state, new(ControlInputKind.SelectAll)).State!;

        var typed = MultilineTextBox.MultilineTextBox.Default.Reduce(selected, ControlInput.FromCharacter('R')).State!;
        var newline = MultilineTextBox.MultilineTextBox.Default.Reduce(selected, new(ControlInputKind.InsertNewline)).State!;

        typed.Text.Should().Be("R");
        newline.Text.Should().Be("\n");
        newline.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void Reduce_moves_between_lines_and_cancels_without_a_state()
    {
        var state = new MultilineTextBoxState("Notes", "First\nSecond", 8, true);

        var moved = MultilineTextBox.MultilineTextBox.Default.Reduce(state, new(ControlInputKind.MoveUp)).State!;
        var cancelled = MultilineTextBox.MultilineTextBox.Default.Reduce(state, new(ControlInputKind.Cancel));

        moved.Cursor.Should().Be(2);
        cancelled.Outcome.Should().Be(MultilineTextBoxOutcome.Cancelled);
        cancelled.State.Should().BeNull();
    }

    [Theory]
    [InlineData(ControlInputKind.MoveLeft, 5)]
    [InlineData(ControlInputKind.MoveRight, 7)]
    [InlineData(ControlInputKind.MoveHome, 5)]
    [InlineData(ControlInputKind.MoveEnd, 7)]
    [InlineData(ControlInputKind.MoveUp, 1)]
    [InlineData(ControlInputKind.MoveDown, 9)]
    public void ReduceEditing_moves_the_cursor_within_and_between_lines(ControlInputKind kind, int expected)
    {
        var state = new MultilineTextBoxState("Notes", "abcd\nxy\n12345", 6, true);

        var result = MultilineTextBox.MultilineTextBox.ReduceEditing(state, new(kind));

        result.Cursor.Should().Be(expected);
    }

    [Fact]
    public void ReduceEditing_inserts_and_deletes_at_the_cursor()
    {
        var state = new MultilineTextBoxState("Notes", "abc", 2, true);

        MultilineTextBox.MultilineTextBox.ReduceEditing(state, new(ControlInputKind.Backspace)).Text.Should().Be("ac");
        MultilineTextBox.MultilineTextBox.ReduceEditing(state, new(ControlInputKind.Delete)).Text.Should().Be("ab");
        MultilineTextBox.MultilineTextBox.ReduceEditing(state, new(ControlInputKind.InsertNewline)).Text.Should().Be("ab\nc");
        MultilineTextBox.MultilineTextBox.ReduceEditing(state, ControlInput.FromCharacter('Z')).Text.Should().Be("abZc");
        MultilineTextBox.MultilineTextBox.ReduceEditing(state, ControlInput.FromCharacter('\0')).Should().Be(state);
    }

    [Fact]
    public void ReduceEditing_collapses_or_replaces_a_selection()
    {
        var state = new MultilineTextBoxState("Notes", "abc", 3, true, SelectionAnchor: 0);

        MultilineTextBox.MultilineTextBox.ReduceEditing(state, new(ControlInputKind.MoveLeft)).Cursor.Should().Be(0);
        MultilineTextBox.MultilineTextBox.ReduceEditing(state, new(ControlInputKind.MoveRight)).Cursor.Should().Be(3);
        MultilineTextBox.MultilineTextBox.ReduceEditing(state, new(ControlInputKind.Backspace)).Text.Should().BeEmpty();
        MultilineTextBox.MultilineTextBox.ReduceEditing(state, new(ControlInputKind.InsertNewline)).Text.Should().Be("\n");
    }

    [Fact]
    public void ReduceEditing_ignores_newline_in_single_line_mode()
    {
        var state = new MultilineTextBoxState("Title", "abc", 3, false);

        MultilineTextBox.MultilineTextBox.ReduceEditing(state, new(ControlInputKind.InsertNewline)).Should().Be(state);
    }

    [Fact]
    public void Insert_uses_the_clamped_cursor_and_clears_selection()
    {
        var state = new MultilineTextBoxState("Notes", "ab", -3, true, SelectionAnchor: 2);

        var result = MultilineTextBox.MultilineTextBox.Insert(state, "X");

        result.Text.Should().Be("Xab");
        result.Cursor.Should().Be(1);
        result.SelectionAnchor.Should().BeNull();
    }

    [Fact]
    public void ReplaceSelection_uses_the_clamped_range_and_clears_selection()
    {
        var state = new MultilineTextBoxState("Notes", "abcde", 2, true, SelectionAnchor: 50);

        var result = MultilineTextBox.MultilineTextBox.ReplaceSelection(state, "X");

        result.Text.Should().Be("abX");
        result.Cursor.Should().Be(3);
        result.SelectionAnchor.Should().BeNull();
    }

    [Theory]
    [InlineData(0, 0, 2)]
    [InlineData(4, 3, 6)]
    [InlineData(6, 3, 6)]
    [InlineData(7, 7, 8)]
    public void Line_boundaries_find_the_current_line(int cursor, int expectedStart, int expectedEnd)
    {
        const string text = "ab\ncde\nf";

        MultilineTextBox.MultilineTextBox.LineStart(text, cursor).Should().Be(expectedStart);
        MultilineTextBox.MultilineTextBox.LineEnd(text, cursor).Should().Be(expectedEnd);
    }

    [Theory]
    [InlineData(3, -1, 3)]
    [InlineData(3, 1, 7)]
    [InlineData(6, -1, 1)]
    [InlineData(6, 1, 9)]
    [InlineData(9, 1, 9)]
    public void MoveLine_preserves_the_column_or_clamps_to_the_target_line(
        int cursor, int offset, int expected)
    {
        MultilineTextBox.MultilineTextBox.MoveLine("abcd\nxy\n12345", cursor, offset).Should().Be(expected);
    }

    [Fact]
    public void Component_measures_and_renders_at_boundary_widths()
    {
        var state = new MultilineTextBoxState("Notes", "\nSecond", 0, true);

        MultilineTextBox.MultilineTextBox.Default.Measure(state, new ControlConstraints(0, 0)).Should().Be(4);
        MultilineTextBox.MultilineTextBox.Default.Render(state, ControlTheme.Default, new ControlConstraints(1, 1)).Should().BeOfType<Panel>();
        MultilineTextBox.MultilineTextBox.Default.Render(state with { SelectionAnchor = 4 }, ControlTheme.Default, new ControlConstraints(30, 4))
            .Should().BeOfType<Panel>();
    }

    [Fact]
    public void CreateSelectedLine_returns_a_selected_text_renderable()
    {
        var state = new MultilineTextBoxState("Notes", "abc\ndef", 6, true, SelectionAnchor: 1);

        MultilineTextBox.MultilineTextBox.CreateSelectedLine("abc", 0, state, ControlTheme.Default)
            .Should().BeOfType<Columns>();
    }
}
