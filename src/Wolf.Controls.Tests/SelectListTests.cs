using FluentAssertions;
using Wolf.Controls.SelectList;

namespace Wolf.Controls.Tests;

public sealed class SelectListTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void FindNextEnabled_returns_minus_one_for_an_empty_list(int direction)
    {
        SelectList.SelectList.FindNextEnabled([], -1, direction).Should().Be(-1);
    }

    [Theory]
    [InlineData(-1, 1, 1)]
    [InlineData(-1, -1, 3)]
    public void FindNextEnabled_starts_at_the_first_or_last_enabled_option_when_unselected(
        int selected, int direction, int expected)
    {
        SelectOption[] options =
        [
            new("Disabled first", IsEnabled: false),
            new("First enabled"),
            new("Disabled middle", IsEnabled: false),
            new("Last enabled")
        ];

        SelectList.SelectList.FindNextEnabled(options, selected, direction).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 1, 4)]
    [InlineData(4, -1, 1)]
    public void FindNextEnabled_skips_disabled_options_in_both_directions(
        int selected, int direction, int expected)
    {
        SelectOption[] options =
        [
            new("Zero"),
            new("One"),
            new("Two", IsEnabled: false),
            new("Three", IsEnabled: false),
            new("Four")
        ];

        SelectList.SelectList.FindNextEnabled(options, selected, direction).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(2, 1)]
    public void FindNextEnabled_keeps_the_selection_when_no_enabled_option_follows(
        int selected, int direction)
    {
        SelectOption[] options =
        [
            new("Zero"),
            new("One", IsEnabled: false),
            new("Two")
        ];

        SelectList.SelectList.FindNextEnabled(options, selected, direction).Should().Be(selected);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    public void FindNextEnabled_keeps_the_selection_when_every_option_is_disabled(
        int selected, int direction)
    {
        SelectOption[] options = [new("One", IsEnabled: false), new("Two", IsEnabled: false)];

        SelectList.SelectList.FindNextEnabled(options, selected, direction).Should().Be(selected);
    }

    [Fact]
    public void Reduce_skips_disabled_options_when_navigating()
    {
        var state = View() with
        {
            Options = [new SelectOption("One"), new SelectOption("Two", IsEnabled: false), new SelectOption("Three")]
        };

        var transition = SelectList.SelectList.Default.Reduce(state, new ControlInput(ControlInputKind.MoveDown));

        transition.Outcome.Should().Be(SelectListOutcome.SelectionChanged);
        transition.State!.SelectedIndex.Should().Be(2);
    }

    [Fact]
    public void Reduce_does_not_accept_a_disabled_or_missing_selection()
    {
        var disabled = View() with { Options = [new SelectOption("One", IsEnabled: false)] };
        var empty = View() with { Options = [] };

        SelectList.SelectList.Default.Reduce(disabled, new ControlInput(ControlInputKind.Accept)).Outcome.Should().Be(SelectListOutcome.Editing);
        SelectList.SelectList.Default.Reduce(empty, new ControlInput(ControlInputKind.Accept)).Outcome.Should().Be(SelectListOutcome.Editing);
    }

    [Fact]
    public void Reduce_emits_accept_and_cancel_outcomes()
    {
        var state = View();

        SelectList.SelectList.Default.Reduce(state, new ControlInput(ControlInputKind.Accept)).Outcome.Should().Be(SelectListOutcome.Accepted);
        var cancelled = SelectList.SelectList.Default.Reduce(state, new ControlInput(ControlInputKind.Cancel));
        cancelled.Outcome.Should().Be(SelectListOutcome.Cancelled);
        cancelled.State.Should().BeNull();
    }

    [Fact]
    public void Component_measures_and_renders_a_constrained_scrolling_list()
    {
        IControl<SelectListState, SelectListOutcome> control = SelectList.SelectList.Default;
        var state = View() with { Options = [new SelectOption("One"), new SelectOption("Two"), new SelectOption("Three")] };

        control.Measure(state, new ControlConstraints(20, 2)).Should().Be(5);
        control.Render(state, ControlTheme.Default, new ControlConstraints(20, 2)).Should().NotBeNull();
    }

    private static SelectListState View() => new("Choose", [new SelectOption("One"), new SelectOption("Two")], 0, null, "None", "Enter SELECT");
}
