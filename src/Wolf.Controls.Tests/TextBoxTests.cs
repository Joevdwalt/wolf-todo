using FluentAssertions;

namespace Wolf.Controls.Tests;

public sealed class TextBoxTests
{
    [Fact]
    public void Reduce_edits_at_the_cursor_and_clamps_navigation()
    {
        var state = TextBoxState.Create("Task", isEditable: true, "Plan review");

        state = TextBox.Default.Reduce(state, new ControlInput(ControlInputKind.MoveHome)).State!;
        state = TextBox.Default.Reduce(state, ControlInput.FromCharacter('A')).State!;
        state = TextBox.Default.Reduce(state, new ControlInput(ControlInputKind.Delete)).State!;
        state = TextBox.Default.Reduce(state, new ControlInput(ControlInputKind.MoveLeft)).State!;

        state.Text.Should().Be("Alan review");
        state.Cursor.Should().Be(0);
        TextBox.DisplayText(state, 6).Should().Be("Alan r");
    }

    [Fact]
    public void Reduce_select_all_replaces_or_deletes_the_selection()
    {
        var state = TextBoxState.Create("Task", isEditable: true, "Plan review");

        var selected = TextBox.Default.Reduce(state, new ControlInput(ControlInputKind.SelectAll)).State!;
        var replaced = TextBox.Default.Reduce(selected, ControlInput.FromCharacter('R')).State!;
        var deleted = TextBox.Default.Reduce(selected, new ControlInput(ControlInputKind.Delete)).State!;

        selected.HasSelection.Should().BeTrue();
        replaced.Text.Should().Be("R");
        deleted.Text.Should().BeEmpty();
    }

    [Fact]
    public void Reduce_accepts_and_cancels_without_changing_the_original_state()
    {
        var state = TextBoxState.Create("Task", isEditable: true, "Plan");

        var accepted = TextBox.Default.Reduce(state, new ControlInput(ControlInputKind.Accept));
        var cancelled = TextBox.Default.Reduce(state, new ControlInput(ControlInputKind.Cancel));

        accepted.Outcome.Should().Be(TextBoxOutcome.Accepted);
        accepted.State.Should().Be(state);
        cancelled.Outcome.Should().Be(TextBoxOutcome.Cancelled);
        cancelled.State.Should().BeNull();
    }

    [Fact]
    public void Reduce_ignores_input_when_read_only()
    {
        var state = TextBoxState.Create("Task", isEditable: false, "Plan");

        var transition = TextBox.Default.Reduce(state, ControlInput.FromCharacter('x'));

        transition.Outcome.Should().Be(TextBoxOutcome.Editing);
        transition.State.Should().BeSameAs(state);
    }

    [Fact]
    public void Component_measures_and_renders_with_clamped_constraints()
    {
        IControl<TextBoxState, TextBoxOutcome> control = TextBox.Default;
        var state = TextBoxState.Create("Task", isEditable: true, "Plan");

        control.Measure(state, new ControlConstraints(0, 0)).Should().Be(TextBox.Height);
        control.Render(state, ControlTheme.Default, new ControlConstraints(0, 0)).Should().NotBeNull();
    }
}
