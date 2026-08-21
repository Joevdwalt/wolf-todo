using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class TodoBulkEditorReducerTests
{
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");
    private readonly TodoBulkEditorReducer reducer = new(() => new DateOnly(2026, 8, 20));

    [Fact]
    public void TryCreateUpdate_parses_all_bulk_modes()
    {
        var state = TodoBulkEditorState.Create(3) with
        {
            ScheduledDate = "t+1",
            Tags = "+#work focus WORK",
            Priority = "high",
            Complete = true
        };

        var valid = reducer.TryCreateUpdate(state, out var update, out var error);

        valid.Should().BeTrue();
        error.Should().BeNull();
        update!.ScheduleMode.Should().Be(TodoBulkScheduleMode.SetDate);
        update.ScheduledDate.Should().Be(new DateOnly(2026, 8, 21));
        update.TagMode.Should().Be(TodoBulkTagMode.Add);
        update.Tags.Should().Equal("work", "focus");
        update.PriorityMode.Should().Be(TodoBulkPriorityMode.Set);
        update.Priority.Should().Be(TodoPriority.High);
        update.Complete.Should().BeTrue();
    }

    [Fact]
    public void Reduce_navigates_toggles_completion_and_accepts_with_save()
    {
        var state = TodoBulkEditorState.Create(2);
        state = reducer.Reduce(state, Key(ConsoleKey.DownArrow), Bindings).State!;
        state = reducer.Reduce(state, Key(ConsoleKey.DownArrow), Bindings).State!;
        state = reducer.Reduce(state, Key(ConsoleKey.DownArrow), Bindings).State!;
        state = reducer.Reduce(state, Key(ConsoleKey.Enter), Bindings).State!;

        var accepted = reducer.Reduce(state, Key(ConsoleKey.S, control: true), Bindings);

        state.SelectedField.Should().Be(TodoBulkEditorField.Completion);
        state.Complete.Should().BeTrue();
        accepted.Outcome.Should().Be(TodoBulkEditorOutcome.Accepted);
        accepted.Update!.Complete.Should().BeTrue();
    }

    [Fact]
    public void Reduce_rejects_an_empty_bulk_draft_and_cancel_preserves_no_update()
    {
        var state = TodoBulkEditorState.Create(2);

        var invalid = reducer.Reduce(state, Key(ConsoleKey.S, control: true), Bindings);
        var cancelled = reducer.Reduce(state, Key(ConsoleKey.Escape), Bindings);

        invalid.Outcome.Should().Be(TodoBulkEditorOutcome.Editing);
        invalid.State!.Error.Should().Contain("at least one");
        cancelled.Outcome.Should().Be(TodoBulkEditorOutcome.Cancelled);
        cancelled.Update.Should().BeNull();
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) =>
        new('\0', key, false, false, control);
}
