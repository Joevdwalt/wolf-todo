using FluentAssertions;
using Spectre.Console;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TodoTitleEditorTests
{
    [Fact]
    public void Reduce_edits_at_the_cursor_and_renders_it_in_place()
    {
        var state = TodoTitleEditor.Create("Plan review");

        state = TodoTitleEditor.Reduce(state, Key(ConsoleKey.Home)).State!;
        state = TodoTitleEditor.Reduce(state, Key('A')).State!;
        state = TodoTitleEditor.Reduce(state, Key(ConsoleKey.Delete)).State!;

        state.Text.Should().Be("Alan review");
        state.Cursor.Should().Be(1);
        TodoTitleEditor.DisplayText(state).Should().Be("A_lan review");
    }

    [Fact]
    public void Reduce_accepts_and_cancels_without_mutating_the_original_state()
    {
        var state = TodoTitleEditor.Create("Prepare workshop");

        var accepted = TodoTitleEditor.Reduce(state, Key(ConsoleKey.Enter));
        var cancelled = TodoTitleEditor.Reduce(state, Key(ConsoleKey.Escape));

        accepted.Outcome.Should().Be(TodoTitleEditorOutcome.Accepted);
        accepted.State!.Text.Should().Be("Prepare workshop");
        cancelled.Outcome.Should().Be(TodoTitleEditorOutcome.Cancelled);
        cancelled.State.Should().BeNull();
    }

    [Fact]
    public void Reduce_ignores_control_characters_for_a_single_line_title()
    {
        var state = TodoTitleEditor.Create("Plan");

        var result = TodoTitleEditor.Reduce(state, Key(ConsoleKey.Tab));

        result.Outcome.Should().Be(TodoTitleEditorOutcome.Editing);
        result.State.Should().Be(state);
    }

    [Fact]
    public void CreateRenderable_returns_only_the_textbox_renderable()
    {
        var renderable = TodoTitleEditor.CreateRenderable(TodoTitleEditor.Create("Plan"), TuiThemes.Wolf);

        renderable.Should().BeOfType<Text>();
    }

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem2, false, false, false);
}
