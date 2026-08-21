using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Controls;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser.Controls;

public sealed class TodoListItemTests
{
    [Fact]
    public void ToggleMark_changes_mark_state_and_emits_a_semantic_outcome()
    {
        var state = State(marked: false);

        var result = TodoListItem.Default.ToggleMark(state);

        result.Outcome.Should().Be(TodoListItemOutcome.MarkToggled);
        result.State!.Row.IsMarked.Should().BeTrue();
    }

    [Fact]
    public void Component_measures_grouped_tag_rows_and_renders_the_item()
    {
        var state = State(marked: true);
        ITuiComponent<TodoListItemState, TodoListItemOutcome> component = TodoListItem.Default;

        component.Measure(state, new TuiComponentConstraints(40, 2)).Should().Be(2);
        component.Render(state, TuiThemes.Wolf, new TuiComponentConstraints(40, 2)).Should().NotBeNull();
    }

    private static TodoListItemState State(bool marked)
    {
        var todo = new TodoItem(
            4,
            false,
            null,
            "Prepare proposal",
            TodoPriority.High,
            ["work"],
            null,
            null,
            string.Empty,
            [],
            []);
        return new TodoListItemState(
            new TodoRow(null, todo, [], true, new TodoIdentity("/work.md", 4))
            {
                IsMarked = marked
            },
            IncludeProject: false,
            ContentWidth: 40);
    }
}
