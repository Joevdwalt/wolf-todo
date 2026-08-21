using FluentAssertions;
using Spectre.Console;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TextBoxTests
{
    [Fact]
    public void Reduce_edits_at_the_cursor()
    {
        var state = TextBox.Create("Task name", editable: true, "Plan review");

        state = TextBox.Reduce(state, Key(ConsoleKey.Home)).State!;
        state = TextBox.Reduce(state, Key('A')).State!;
        state = TextBox.Reduce(state, Key(ConsoleKey.Delete)).State!;

        state.Text.Should().Be("Alan review");
        state.Cursor.Should().Be(1);
        TextBox.DisplayText(state, 12).Should().Be("Alan review ");
    }

    [Fact]
    public void Reduce_selects_all_and_printable_input_replaces_the_selection()
    {
        var state = TextBox.Create("Task name", editable: true, "Plan review");

        var selected = TextBox.Reduce(state, Key(ConsoleKey.A, control: true)).State!;
        var replaced = TextBox.Reduce(selected, Key('R')).State!;

        selected.HasSelection.Should().BeTrue();
        selected.SelectionStart.Should().Be(0);
        selected.SelectionLength.Should().Be(state.Text.Length);
        replaced.Text.Should().Be("R");
        replaced.Cursor.Should().Be(1);
        replaced.HasSelection.Should().BeFalse();
    }

    [Theory]
    [InlineData(ConsoleKey.Backspace)]
    [InlineData(ConsoleKey.Delete)]
    public void Reduce_deletes_the_complete_selection(ConsoleKey key)
    {
        var state = TextBox.Create("Task name", editable: true, "Plan review");
        state = TextBox.Reduce(state, Key(ConsoleKey.A, control: true)).State!;

        var deleted = TextBox.Reduce(state, Key(key)).State!;

        deleted.Text.Should().BeEmpty();
        deleted.Cursor.Should().Be(0);
        deleted.HasSelection.Should().BeFalse();
    }

    [Theory]
    [InlineData(ConsoleKey.LeftArrow, 0)]
    [InlineData(ConsoleKey.Home, 0)]
    [InlineData(ConsoleKey.RightArrow, 11)]
    [InlineData(ConsoleKey.End, 11)]
    public void Reduce_collapses_the_selection_in_the_navigation_direction(ConsoleKey key, int expectedCursor)
    {
        var state = TextBox.Create("Task name", editable: true, "Plan review");
        state = TextBox.Reduce(state, Key(ConsoleKey.A, control: true)).State!;

        var moved = TextBox.Reduce(state, Key(key)).State!;

        moved.Cursor.Should().Be(expectedCursor);
        moved.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void Reduce_accepts_and_cancels_without_mutating_the_original_state()
    {
        var state = TextBox.Create("Task name", editable: true, "Prepare workshop");

        var accepted = TextBox.Reduce(state, Key(ConsoleKey.Enter));
        var cancelled = TextBox.Reduce(state, Key(ConsoleKey.Escape));

        accepted.Outcome.Should().Be(TextBoxOutcome.Accepted);
        accepted.State!.Text.Should().Be("Prepare workshop");
        cancelled.Outcome.Should().Be(TextBoxOutcome.Cancelled);
        cancelled.State.Should().BeNull();
    }

    [Fact]
    public void DisplayText_keeps_the_cursor_visible_when_the_value_exceeds_the_box_width()
    {
        var state = TextBox.Create("Task name", editable: true, "Prepare workshop");

        TextBox.DisplayText(state, 6).Should().Be("kshop ");
    }

    [Fact]
    public void DisplayText_keeps_the_active_end_of_a_selection_visible()
    {
        var state = TextBox.Create("Task name", editable: true, "Prepare workshop");
        state = TextBox.Reduce(state, Key(ConsoleKey.A, control: true)).State!;

        TextBox.DisplayText(state, 6).Should().Be("rkshop");
        TextBox.CreateRenderable(state, TuiThemes.Wolf, 8).Should().BeOfType<Rows>();
    }

    [Fact]
    public void Reduce_ignores_all_input_when_the_textbox_is_read_only()
    {
        var state = TextBox.Create("Task name", editable: false, "Prepare workshop");

        var typed = TextBox.Reduce(state, Key('x'));
        var accepted = TextBox.Reduce(state, Key(ConsoleKey.Enter));
        var cancelled = TextBox.Reduce(state, Key(ConsoleKey.Escape));
        var selected = TextBox.Reduce(state, Key(ConsoleKey.A, control: true));

        typed.Outcome.Should().Be(TextBoxOutcome.Editing);
        typed.State.Should().Be(state);
        accepted.Outcome.Should().Be(TextBoxOutcome.Editing);
        accepted.State.Should().Be(state);
        cancelled.Outcome.Should().Be(TextBoxOutcome.Editing);
        cancelled.State.Should().Be(state);
        selected.State.Should().Be(state);
        selected.State!.HasSelection.Should().BeFalse();
    }

    [Fact]
    public void DisplayText_hides_the_cursor_when_the_textbox_is_read_only()
    {
        var state = TextBox.Create("Task name", editable: false, "Prepare workshop");

        TextBox.DisplayText(state, 10).Should().Be("Prepare wo");
    }

    [Fact]
    public void CreateRenderable_builds_a_label_and_rounded_input_box()
    {
        var renderable = TextBox.CreateRenderable(TextBox.Create("Task name", editable: true, "Plan"), TuiThemes.Wolf, 20);

        renderable.Should().BeOfType<Rows>();
    }

    [Fact]
    public void Create_records_the_active_state_independently_from_editability()
    {
        var activeReadOnly = TextBox.Create("Task name", editable: false, "Plan", isActive: true);
        var inactiveEditable = TextBox.Create("Task name", editable: true, "Plan");

        activeReadOnly.Edit.Should().BeFalse();
        activeReadOnly.IsActive.Should().BeTrue();
        inactiveEditable.Edit.Should().BeTrue();
        inactiveEditable.IsActive.Should().BeFalse();
        activeReadOnly.Label.Should().Be("Task name");
    }

    [Fact]
    public void Component_contract_measures_renders_and_emits_textbox_outcomes()
    {
        ITuiComponent<TextBoxState, TextBoxOutcome> component = TextBox.Default;
        var state = TextBox.Create("Task name", editable: true, "Plan");

        var transition = component.Reduce(state, Key(ConsoleKey.Enter), TuiKeyBindings.CreateDefaults(":q"));

        component.Measure(state, new TuiComponentConstraints(20, 1)).Should().Be(TextBox.Height);
        component.Render(state, TuiThemes.Wolf, new TuiComponentConstraints(20, 1)).Should().BeOfType<Rows>();
        transition.Outcome.Should().Be(TextBoxOutcome.Accepted);
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) => new('\0', key, false, false, control);

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem2, false, false, false);
}
