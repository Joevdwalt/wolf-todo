using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class ApplicationCommandReducerTests
{
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");
    private readonly ApplicationCommandReducer reducer = new();

    [Fact]
    public void Reduce_opens_and_submits_the_global_quit_command()
    {
        var opened = reducer.Reduce(ApplicationCommandState.Initial, Key(':'), Bindings).State;
        var typed = reducer.Reduce(opened, Key('q'), Bindings).State;
        var submitted = reducer.Reduce(typed, Key(ConsoleKey.Enter), Bindings);

        opened.Should().Be(new ApplicationCommandState(true, ":", null));
        submitted.Operation.Should().Be(ApplicationCommandOperation.Exit);
        submitted.State.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Reduce_exposes_completed_and_unknown_command_results()
    {
        var completed = reducer.Reduce(
            new ApplicationCommandState(true, ":completed", null),
            Key(ConsoleKey.Enter),
            Bindings);
        var unknown = reducer.Reduce(
            new ApplicationCommandState(true, ":wat", null),
            Key(ConsoleKey.Enter),
            Bindings);

        completed.Operation.Should().Be(ApplicationCommandOperation.ToggleCompleted);
        unknown.Operation.Should().Be(ApplicationCommandOperation.None);
        unknown.State.Error.Should().Be("Unknown command: :wat");
    }

    [Fact]
    public void Reduce_opens_the_command_palette_with_the_help_command()
    {
        var result = reducer.Reduce(
            new ApplicationCommandState(true, ":help", null),
            Key(ConsoleKey.Enter),
            Bindings);

        result.Operation.Should().Be(ApplicationCommandOperation.OpenPalette);
        result.State.Error.Should().BeNull();
    }

    [Fact]
    public void Reduce_completes_a_unique_command_prefix_with_tab()
    {
        var result = reducer.Reduce(
            new ApplicationCommandState(true, ":roll", null),
            Key(ConsoleKey.Tab),
            Bindings);

        result.State.Value.Should().Be(":roll-today");
        result.State.CompletionSeed.Should().BeNull();
    }

    [Fact]
    public void Reduce_cycles_ambiguous_command_completions_and_resets_after_typing()
    {
        var state = new ApplicationCommandState(true, ":", null);

        var first = reducer.Reduce(state, Key(ConsoleKey.Tab), Bindings);
        var second = reducer.Reduce(first.State, Key(ConsoleKey.Tab), Bindings);
        var typed = reducer.Reduce(second.State, Key('x'), Bindings);

        first.State.Value.Should().Be(":completed");
        second.State.Value.Should().Be(":help");
        typed.State.Value.Should().Be(":helpx");
        typed.State.CompletionSeed.Should().BeNull();
        typed.State.CompletionIndex.Should().Be(-1);
    }

    [Fact]
    public void Reduce_completes_configured_commands()
    {
        var bindings = Bindings with { ToggleCompletedCommand = ":done" };

        var result = reducer.Reduce(
            new ApplicationCommandState(true, ":do", null),
            Key(ConsoleKey.Tab),
            bindings);

        result.State.Value.Should().Be(":done");
    }

    [Fact]
    public void Reduce_submits_the_roll_today_command()
    {
        var result = reducer.Reduce(
            new ApplicationCommandState(true, ":roll-today", null),
            Key(ConsoleKey.Enter),
            Bindings);

        result.Operation.Should().Be(ApplicationCommandOperation.RollProjectToday);
        result.State.Should().Be(ApplicationCommandState.Initial);
    }

    [Fact]
    public void Reduce_parses_a_project_title_for_the_move_todo_command()
    {
        var result = reducer.Reduce(
            new ApplicationCommandState(true, ":move-todo-project Personal Admin", null),
            Key(ConsoleKey.Enter),
            Bindings);

        result.Operation.Should().Be(ApplicationCommandOperation.MoveTodoProject);
        result.ProjectTitle.Should().Be("Personal Admin");
    }

    [Fact]
    public void Reduce_cancels_and_keeps_the_colon_when_backspacing()
    {
        var backed = reducer.Reduce(
            new ApplicationCommandState(true, ":", null),
            Key(ConsoleKey.Backspace),
            Bindings);
        var cancelled = reducer.Reduce(backed.State, Key(ConsoleKey.Escape), Bindings);

        backed.State.Value.Should().Be(":");
        cancelled.State.Should().Be(ApplicationCommandState.Initial);
    }

    private static ConsoleKeyInfo Key(char character) =>
        new(character, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) =>
        new('\0', key, false, false, false);
}
