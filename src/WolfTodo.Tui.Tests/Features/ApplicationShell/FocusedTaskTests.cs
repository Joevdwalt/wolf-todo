using System.Collections.Immutable;
using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class FocusedTaskTests
{
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");

    [Fact]
    public void Presenter_flattens_the_root_and_nested_checklist_and_keeps_selection()
    {
        var child = Todo(5, "Child", subtasks: [Todo(6, "Grandchild")]);
        var root = Todo(3, "Root", subtasks: [child]);
        var state = FocusedTaskState.Create(new TodoIdentity("/work.md", 3), root) with
        {
            SelectedIdentity = new TodoIdentity("/work.md", 6)
        };

        var view = new FocusedTaskPresenter().CreateView(Catalog(root), state)!;

        view.Items.Select(item => item.Todo.Title).Should().Equal("Root", "Child", "Grandchild");
        view.SelectedItem.Todo.Title.Should().Be("Grandchild");
        view.Items.Select(item => item.TreePath.Length).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Reducer_moves_the_highlight_and_targets_completion_at_the_selected_subtask()
    {
        var root = Todo(3, "Root", subtasks: [Todo(5, "Child")]);
        var state = FocusedTaskState.Create(new TodoIdentity("/work.md", 3), root);
        var presenter = new FocusedTaskPresenter();
        var reducer = new FocusedTaskReducer();

        var moved = reducer.Reduce(state, Key('j'), Bindings, presenter.CreateView(Catalog(root), state)!);
        var movedView = presenter.CreateView(Catalog(root), moved.State)!;
        var toggled = reducer.Reduce(moved.State, Key(ConsoleKey.Spacebar), Bindings, movedView);

        moved.State.SelectedIdentity.Should().Be(new TodoIdentity("/work.md", 5));
        toggled.Operation.Should().Be(FocusedTaskOperation.ToggleCompleted);
        toggled.Identity.Should().Be(new TodoIdentity("/work.md", 5));
        toggled.ExpectedTodo!.Title.Should().Be("Child");
    }

    [Fact]
    public void Reducer_opens_the_unified_editor_with_uppercase_e_and_exits_with_escape()
    {
        var root = Todo(3, "Root");
        var state = FocusedTaskState.Create(new TodoIdentity("/work.md", 3), root);
        var view = new FocusedTaskPresenter().CreateView(Catalog(root), state)!;
        var reducer = new FocusedTaskReducer();

        var edited = reducer.Reduce(state, Key('E'), Bindings, view);
        var exited = reducer.Reduce(state, Key(ConsoleKey.Escape), Bindings, view);

        edited.State.Editor.Should().NotBeNull();
        exited.Operation.Should().Be(FocusedTaskOperation.Exit);
    }

    private static ProjectCatalog Catalog(TodoItem root) => new(
        [new TodoProject("Work", "/work.md", [root])],
        []);

    private static TodoItem Todo(
        int sourceLine,
        string title,
        ImmutableArray<TodoItem> subtasks = default) => new(
        sourceLine,
        false,
        null,
        title,
        null,
        [],
        null,
        null,
        string.Empty,
        [],
        subtasks.IsDefault ? [] : subtasks);

    private static ConsoleKeyInfo Key(char character) =>
        new(character, char.ToUpperInvariant(character) switch
        {
            'E' => ConsoleKey.E,
            'J' => ConsoleKey.J,
            _ => ConsoleKey.NoName
        }, char.IsUpper(character), false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);
}
