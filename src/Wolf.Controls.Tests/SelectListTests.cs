using FluentAssertions;

namespace Wolf.Controls.Tests;

public sealed class SelectListTests
{
    [Fact]
    public void Reduce_skips_disabled_options_when_navigating()
    {
        var state = View() with
        {
            Options = [new SelectOption("One"), new SelectOption("Two", IsEnabled: false), new SelectOption("Three")]
        };

        var transition = SelectList.Default.Reduce(state, new ControlInput(ControlInputKind.MoveDown));

        transition.Outcome.Should().Be(SelectListOutcome.SelectionChanged);
        transition.State!.SelectedIndex.Should().Be(2);
    }

    [Fact]
    public void Reduce_does_not_accept_a_disabled_or_missing_selection()
    {
        var disabled = View() with { Options = [new SelectOption("One", IsEnabled: false)] };
        var empty = View() with { Options = [] };

        SelectList.Default.Reduce(disabled, new ControlInput(ControlInputKind.Accept)).Outcome.Should().Be(SelectListOutcome.Editing);
        SelectList.Default.Reduce(empty, new ControlInput(ControlInputKind.Accept)).Outcome.Should().Be(SelectListOutcome.Editing);
    }

    [Fact]
    public void Reduce_emits_accept_and_cancel_outcomes()
    {
        var state = View();

        SelectList.Default.Reduce(state, new ControlInput(ControlInputKind.Accept)).Outcome.Should().Be(SelectListOutcome.Accepted);
        var cancelled = SelectList.Default.Reduce(state, new ControlInput(ControlInputKind.Cancel));
        cancelled.Outcome.Should().Be(SelectListOutcome.Cancelled);
        cancelled.State.Should().BeNull();
    }

    [Fact]
    public void Component_measures_and_renders_a_constrained_scrolling_list()
    {
        IControl<SelectListState, SelectListOutcome> control = SelectList.Default;
        var state = View() with { Options = [new SelectOption("One"), new SelectOption("Two"), new SelectOption("Three")] };

        control.Measure(state, new ControlConstraints(20, 2)).Should().Be(5);
        control.Render(state, ControlTheme.Default, new ControlConstraints(20, 2)).Should().NotBeNull();
    }

    private static SelectListState View() => new("Choose", [new SelectOption("One"), new SelectOption("Two")], 0, null, "None", "Enter SELECT");
}
