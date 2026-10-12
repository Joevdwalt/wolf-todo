using FluentAssertions;
using Wolf.Controls.MultilineTextBox;
using Wolf.Controls.SelectList;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TuiControlAdapterTests
{
    [Fact]
    public void Theme_mapping_preserves_wtodo_semantic_roles()
    {
        var theme = TuiControlAdapter.ToControlTheme(TuiThemes.Wolf);

        theme.Accent.Should().Be(TuiThemes.Wolf.Accent);
        theme.SecondaryText.Should().Be(TuiThemes.Wolf.SecondaryText);
        theme.Surface.Should().Be(TuiThemes.Wolf.Surface);
        theme.Error.Should().Be(TuiThemes.Wolf.Error);
    }

    [Fact]
    public void List_input_maps_configured_navigation_and_characters()
    {
        var bindings = TuiKeyBindings.CreateDefaults(":q");

        TuiControlAdapter.ToListInput(new ConsoleKeyInfo('j', ConsoleKey.J, false, false, false), bindings).Kind
            .Should().Be(ControlInputKind.MoveDown);
        TuiControlAdapter.ToListInput(new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false), bindings)
            .Should().Be(ControlInput.FromCharacter('x'));
    }

    [Fact]
    public void Multiline_input_uses_the_configured_save_binding_and_keeps_enter_as_newline()
    {
        var bindings = TuiKeyBindings.CreateDefaults(":q") with
        {
            SaveForm = [KeyGesture.Parse("Ctrl+X")]
        };
        var state = MultilineTextBoxState.Create("Notes", "Plan", true);

        var newline = TuiControlAdapter.ReduceMultilineTextBox(state, Key(ConsoleKey.Enter), bindings);
        var ignored = TuiControlAdapter.ReduceMultilineTextBox(state, Key(ConsoleKey.S, control: true), bindings);
        var saved = TuiControlAdapter.ReduceMultilineTextBox(state, Key(ConsoleKey.X, control: true), bindings);

        newline.State!.Text.Should().Be("Plan\n");
        ignored.Outcome.Should().Be(MultilineTextBoxOutcome.Editing);
        saved.Outcome.Should().Be(MultilineTextBoxOutcome.Accepted);
    }

    [Fact]
    public void List_input_uses_configured_navigation_and_skips_disabled_options()
    {
        var bindings = TuiKeyBindings.CreateDefaults(":q") with
        {
            MoveDown = [KeyGesture.Parse("n")],
            Open = [KeyGesture.Parse("Enter")]
        };
        var state = new SelectListState("Choose", [new SelectOption("One"), new SelectOption("Two", IsEnabled: false), new SelectOption("Three")], 0, null, "None", "Select");

        var moved = TuiControlAdapter.ReduceSelectList(state, new ConsoleKeyInfo('n', ConsoleKey.N, false, false, false), bindings);
        var accepted = TuiControlAdapter.ReduceSelectList(moved.State!, Key(ConsoleKey.Enter), bindings);

        moved.State!.SelectedIndex.Should().Be(2);
        accepted.Outcome.Should().Be(SelectListOutcome.Accepted);
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) => new('\0', key, false, false, control);
}
