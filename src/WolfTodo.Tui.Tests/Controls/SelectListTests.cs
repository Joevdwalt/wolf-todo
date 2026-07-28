using FluentAssertions;
using Spectre.Console;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class SelectListTests
{
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");

    [Fact]
    public void Reduce_emits_selection_accept_and_cancel_outcomes()
    {
        var state = View();

        var moved = SelectList.Default.Reduce(state, Key('j'), Bindings);
        var accepted = SelectList.Default.Reduce(moved.State!, Key('l'), Bindings);
        var cancelled = SelectList.Default.Reduce(state, Key('h'), Bindings);

        moved.Outcome.Should().Be(SelectListOutcome.SelectionChanged);
        moved.State!.SelectedIndex.Should().Be(1);
        accepted.Outcome.Should().Be(SelectListOutcome.Accepted);
        cancelled.Outcome.Should().Be(SelectListOutcome.Cancelled);
        cancelled.State.Should().BeNull();
    }

    [Fact]
    public void Component_measures_and_renders_with_a_scrolling_row_constraint()
    {
        ITuiComponent<SelectListView, SelectListOutcome> component = SelectList.Default;
        var state = View() with { Options = [new SelectOption("One"), new SelectOption("Two"), new SelectOption("Three")] };

        component.Measure(state, new TuiComponentConstraints(30, 2)).Should().Be(5);
        component.Render(state, TuiThemes.Wolf, new TuiComponentConstraints(30, 2)).Should().NotBeNull();
    }

    private static SelectListView View() => new(
        "Choose",
        [new SelectOption("One"), new SelectOption("Two")],
        0,
        null,
        "None",
        "Enter SELECT  Esc CANCEL");

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem2, false, false, false);
}
