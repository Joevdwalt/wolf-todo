using System.Collections.Immutable;
using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class BrowserReducerTests
{
    private readonly BrowserReducer reducer = new();
    private static readonly ApplicationConfiguration Configuration = new([], ":q");

    [Fact]
    public void Reduce_moves_from_projects_to_todos_on_enter()
    {
        var result = reducer.Reduce(BrowserState.Initial, Key(ConsoleKey.Enter), Configuration, EmptyView());

        result.State.Focus.Should().Be(BrowserFocus.Todos);
    }

    [Fact]
    public void Reduce_opens_filter_mode_with_the_committed_filter()
    {
        var state = BrowserState.Initial with { FilterText = "renewal" };

        var result = reducer.Reduce(state, Key('/'), Configuration, EmptyView());

        result.State.IsFilterMode.Should().BeTrue();
        result.State.FilterDraft.Should().Be("renewal");
    }

    [Fact]
    public void Reduce_updates_the_filter_draft_and_resets_todo_selection_while_typing()
    {
        var state = BrowserState.Initial with
        {
            IsFilterMode = true,
            FilterDraft = "rene",
            TodoIndex = 3
        };

        var result = reducer.Reduce(state, Key('w'), Configuration, EmptyView());

        result.State.FilterDraft.Should().Be("renew");
        result.State.TodoIndex.Should().Be(0);
    }

    [Fact]
    public void Reduce_commits_a_trimmed_filter_on_enter()
    {
        var state = BrowserState.Initial with { IsFilterMode = true, FilterDraft = "  renewal  " };

        var result = reducer.Reduce(state, Key(ConsoleKey.Enter), Configuration, EmptyView());

        result.State.IsFilterMode.Should().BeFalse();
        result.State.FilterText.Should().Be("renewal");
        result.State.FilterDraft.Should().Be("renewal");
    }

    [Fact]
    public void Reduce_clears_the_filter_when_an_empty_draft_is_submitted()
    {
        var state = BrowserState.Initial with
        {
            IsFilterMode = true,
            FilterText = "renewal",
            FilterDraft = string.Empty
        };

        var result = reducer.Reduce(state, Key(ConsoleKey.Enter), Configuration, EmptyView());

        result.State.FilterText.Should().BeEmpty();
    }

    [Fact]
    public void Reduce_restores_the_committed_filter_when_filter_editing_is_cancelled()
    {
        var state = BrowserState.Initial with
        {
            IsFilterMode = true,
            FilterText = "renewal",
            FilterDraft = "replacement"
        };

        var result = reducer.Reduce(state, Key(ConsoleKey.Escape), Configuration, EmptyView());

        result.State.IsFilterMode.Should().BeFalse();
        result.State.FilterText.Should().Be("renewal");
        result.State.FilterDraft.Should().Be("renewal");
    }

    [Fact]
    public void Reduce_moves_with_default_vim_navigation_keys()
    {
        var movedDown = reducer.Reduce(BrowserState.Initial, Key('j'), Configuration, NavigationView());
        var movedUp = reducer.Reduce(movedDown.State, Key('k'), Configuration, NavigationView());

        movedDown.State.ProjectIndex.Should().Be(1);
        movedUp.State.ProjectIndex.Should().Be(0);
    }

    [Fact]
    public void Reduce_jumps_to_the_top_and_bottom_of_the_focused_list()
    {
        var projects = NavigationView();
        var bottomProject = reducer.Reduce(BrowserState.Initial, Key('G'), Configuration, projects);
        var topProject = reducer.Reduce(bottomProject.State, Key('g'), Configuration, projects);
        var todos = TodoNavigationView(3);
        var todoState = BrowserState.Initial with { Focus = BrowserFocus.Todos, TodoIndex = 1 };
        var bottomTodo = reducer.Reduce(todoState, Key('G'), Configuration, todos);
        var topTodo = reducer.Reduce(bottomTodo.State, Key('g'), Configuration, todos);

        bottomProject.State.ProjectIndex.Should().Be(1);
        topProject.State.ProjectIndex.Should().Be(0);
        bottomTodo.State.TodoIndex.Should().Be(2);
        topTodo.State.TodoIndex.Should().Be(0);
    }

    [Fact]
    public void Reduce_opens_and_goes_back_with_default_vim_navigation_keys()
    {
        var opened = reducer.Reduce(BrowserState.Initial, Key('l'), Configuration, EmptyView());
        var backed = reducer.Reduce(opened.State, Key('h'), Configuration, EmptyView());

        opened.State.Focus.Should().Be(BrowserFocus.Todos);
        backed.State.Focus.Should().Be(BrowserFocus.Projects);
    }

    [Fact]
    public void Reduce_uses_replacement_bindings_instead_of_their_defaults()
    {
        var bindings = Configuration.KeyBindings with { MoveDown = [KeyGesture.Parse("n")] };
        var configuration = new ApplicationConfiguration([], bindings);

        var ignored = reducer.Reduce(BrowserState.Initial, Key('j'), configuration, NavigationView());
        var handled = reducer.Reduce(BrowserState.Initial, Key('n'), configuration, NavigationView());

        ignored.State.ProjectIndex.Should().Be(0);
        handled.State.ProjectIndex.Should().Be(1);
    }

    [Fact]
    public void Reduce_uses_configured_focus_open_and_back_bindings()
    {
        var bindings = Configuration.KeyBindings with
        {
            FocusNext = [KeyGesture.Parse("Ctrl+N")],
            FocusPrevious = [KeyGesture.Parse("Ctrl+P")],
            Open = [KeyGesture.Parse("o")],
            Back = [KeyGesture.Parse("b")]
        };
        var configuration = new ApplicationConfiguration([], bindings);

        var next = reducer.Reduce(
            BrowserState.Initial,
            Key(ConsoleKey.N, control: true),
            configuration,
            EmptyView());
        var previous = reducer.Reduce(
            BrowserState.Initial,
            Key(ConsoleKey.P, control: true),
            configuration,
            EmptyView());
        var opened = reducer.Reduce(BrowserState.Initial, Key('o'), configuration, EmptyView());
        var backed = reducer.Reduce(opened.State, Key('b'), configuration, EmptyView());

        next.State.Focus.Should().Be(BrowserFocus.Todos);
        previous.State.Focus.Should().Be(BrowserFocus.Details);
        opened.State.Focus.Should().Be(BrowserFocus.Todos);
        backed.State.Focus.Should().Be(BrowserFocus.Projects);
    }

    [Fact]
    public void Reduce_uses_the_configured_filter_mode_launcher()
    {
        var bindings = Configuration.KeyBindings with
        {
            FilterMode = [KeyGesture.Parse("Ctrl+F")]
        };
        var configuration = new ApplicationConfiguration([], bindings);

        var filter = reducer.Reduce(BrowserState.Initial, Key(ConsoleKey.F, control: true), configuration, EmptyView());

        filter.State.IsFilterMode.Should().BeTrue();
    }

    [Fact]
    public void Reduce_opens_sort_mode_with_the_configured_launcher()
    {
        var result = reducer.Reduce(BrowserState.Initial, Key('t'), Configuration, EmptyView());

        result.State.IsSortMode.Should().BeTrue();
    }

    [Theory]
    [InlineData('n', TodoSortProperty.Name, TodoSortDirection.Ascending)]
    [InlineData('N', TodoSortProperty.Name, TodoSortDirection.Descending)]
    [InlineData('d', TodoSortProperty.Schedule, TodoSortDirection.Ascending)]
    [InlineData('D', TodoSortProperty.Schedule, TodoSortDirection.Descending)]
    [InlineData('t', TodoSortProperty.Tags, TodoSortDirection.Ascending)]
    [InlineData('T', TodoSortProperty.Tags, TodoSortDirection.Descending)]
    [InlineData('f', TodoSortProperty.File, TodoSortDirection.Ascending)]
    [InlineData('F', TodoSortProperty.File, TodoSortDirection.Descending)]
    [InlineData('p', TodoSortProperty.Priority, TodoSortDirection.Ascending)]
    [InlineData('P', TodoSortProperty.Priority, TodoSortDirection.Descending)]
    public void Reduce_applies_sort_dialog_choices(
        char key,
        TodoSortProperty property,
        TodoSortDirection direction)
    {
        var identity = new TodoIdentity("/alpha.md", 4);
        var state = BrowserState.Initial with { IsSortMode = true };

        var result = reducer.Reduce(state, Key(key), Configuration, SelectedView(identity));

        result.State.IsSortMode.Should().BeFalse();
        result.State.Sort.Should().Be(new TodoSort(property, direction));
        result.State.PendingTodoSelection.Should().Be(identity);
    }

    [Fact]
    public void Reduce_restores_source_order_from_the_sort_dialog()
    {
        var state = BrowserState.Initial with
        {
            IsSortMode = true,
            Sort = new TodoSort(TodoSortProperty.Name, TodoSortDirection.Descending)
        };

        var result = reducer.Reduce(state, Key('o'), Configuration, EmptyView());

        result.State.Sort.Should().Be(TodoSort.Source);
        result.State.IsSortMode.Should().BeFalse();
    }

    [Fact]
    public void Reduce_cancels_or_ignores_sort_dialog_input()
    {
        var state = BrowserState.Initial with { IsSortMode = true };

        var ignored = reducer.Reduce(state, Key('x'), Configuration, EmptyView());
        var cancelled = reducer.Reduce(state, Key(ConsoleKey.Escape), Configuration, EmptyView());

        ignored.State.IsSortMode.Should().BeTrue();
        ignored.State.Sort.Should().Be(TodoSort.Source);
        cancelled.State.IsSortMode.Should().BeFalse();
        cancelled.State.Sort.Should().Be(TodoSort.Source);
    }

    [Fact]
    public void Reduce_requests_completion_for_the_selected_todo()
    {
        var identity = new TodoIdentity("/alpha.md", 4);

        var result = reducer.Reduce(
            BrowserState.Initial,
            Key(ConsoleKey.Spacebar),
            Configuration,
            SelectedView(identity));

        result.Operation.Should().Be(BrowserOperation.ToggleCompleted);
        result.TodoIdentity.Should().Be(identity);
    }

    [Fact]
    public void Reduce_opens_and_saves_a_create_form_for_an_individual_project()
    {
        var project = new TodoProject("Alpha", "/alpha.md", []);
        var view = new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("Alpha", 0, project, null, true)],
            [],
            null,
            "Alpha",
            project.Path,
            null,
            string.Empty);
        var opened = reducer.Reduce(BrowserState.Initial, Key('a'), Configuration, view);
        var withTitle = opened.State with
        {
            Editor = opened.State.Editor! with
            {
                Values = new TodoUpdate("New task", null, null, [], null, null),
                ScheduledDate = "2026-07-15",
                ScheduledTime = "09:30"
            }
        };

        var saved = reducer.Reduce(
            withTitle,
            Key(ConsoleKey.S, control: true),
            Configuration,
            view);

        saved.Operation.Should().Be(BrowserOperation.Create);
        saved.ProjectPath.Should().Be(project.Path);
        saved.Update!.Fields.Title.Should().Be("New task");
        saved.Update.Fields.Schedule.Should().Be(
            new TodoSchedule(new DateOnly(2026, 7, 15), new TimeOnly(9, 30)));
        saved.State.Editor.Should().NotBeNull("the application clears it after a successful write");
    }

    [Theory]
    [InlineData("2026-07-15", "", "both be set")]
    [InlineData("", "09:30", "both be set")]
    [InlineData("2026-07-15", "09:15", "half-hour")]
    [InlineData("2026-07-15", "22:00", "half-hour")]
    public void Reduce_rejects_incomplete_or_invalid_schedules(
        string date,
        string time,
        string expectedError)
    {
        var project = new TodoProject("Alpha", "/alpha.md", []);
        var view = new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("Alpha", 0, project, null, true)],
            [], null, "Alpha", project.Path, null, string.Empty);
        var opened = reducer.Reduce(BrowserState.Initial, Key('a'), Configuration, view);
        var state = opened.State with
        {
            Editor = opened.State.Editor! with
            {
                Values = new TodoUpdate("New task", null, null, [], null, null),
                ScheduledDate = date,
                ScheduledTime = time
            }
        };

        var saved = reducer.Reduce(state, Key(ConsoleKey.S, control: true), Configuration, view);

        saved.Operation.Should().Be(BrowserOperation.None);
        saved.State.Editor!.Error.Should().Contain(expectedError);
    }

    [Fact]
    public void Reduce_edits_notes_and_subtasks_as_one_content_update()
    {
        var identity = new TodoIdentity("/alpha.md", 1);
        var child = new TodoItem(3, false, null, "Child", null, [], null, null, string.Empty, [], []);
        var todo = new TodoItem(
            1, false, null, "Parent", null, [], null, null, string.Empty,
            [new TodoNote(2, "Existing note")], [child]);
        var view = SelectedView(identity, todo);

        var opened = reducer.Reduce(BrowserState.Initial, Key('E'), Configuration, view);
        var contentSelected = opened.State with
        {
            Editor = opened.State.Editor! with { SelectedIndex = TodoTaskEditorState.FieldCount }
        };
        var adding = reducer.Reduce(contentSelected, Key('a'), Configuration, view);
        var choosing = reducer.Reduce(adding.State, Key(ConsoleKey.Enter), Configuration, view);
        var typed = reducer.Reduce(choosing.State, Key('N'), Configuration, view);
        var accepted = reducer.Reduce(typed.State, Key(ConsoleKey.Enter), Configuration, view);
        var subtask = reducer.Reduce(accepted.State, Key('j'), Configuration, view);
        var toggled = reducer.Reduce(subtask.State, Key(ConsoleKey.Spacebar), Configuration, view);
        var saved = reducer.Reduce(toggled.State, Key(ConsoleKey.S, control: true), Configuration, view);

        opened.State.Editor.Should().NotBeNull();
        saved.Operation.Should().Be(BrowserOperation.Update);
        saved.Update!.Content.Items.Should().HaveCount(3);
        saved.Update.Content.Items.OfType<TodoNoteUpdate>()
            .Select(note => note.Text).Should().Equal("Existing note", "N");
        saved.Update.Content.Items.OfType<TodoSubtaskUpdate>()
            .Should().ContainSingle().Which.IsCompleted.Should().BeTrue();
        saved.State.Editor.Should().NotBeNull("the application clears it after a successful write");
    }

    [Fact]
    public void Reduce_adds_a_chosen_subtask_after_the_outline_selection()
    {
        var identity = new TodoIdentity("/alpha.md", 1);
        var child = new TodoItem(3, false, null, "Existing child", null, [], null, null, string.Empty, [], []);
        var todo = new TodoItem(
            1, false, null, "Parent", null, [], null, null, string.Empty,
            [new TodoNote(2, "Opening note")], [child]);
        var view = SelectedView(identity, todo);

        var opened = reducer.Reduce(BrowserState.Initial, Key('E'), Configuration, view);
        var contentSelected = opened.State with
        {
            Editor = opened.State.Editor! with { SelectedIndex = TodoTaskEditorState.FieldCount }
        };
        var picker = reducer.Reduce(contentSelected, Key('a'), Configuration, view);
        var subtaskType = reducer.Reduce(picker.State, Key('j'), Configuration, view);
        var editing = reducer.Reduce(subtaskType.State, Key('l'), Configuration, view);
        var typed = reducer.Reduce(editing.State, Key('N'), Configuration, view);
        var accepted = reducer.Reduce(typed.State, Key(ConsoleKey.Enter), Configuration, view);

        picker.State.Editor!.Mode.Should().Be(TodoTaskEditorMode.ChooseContentType);
        subtaskType.State.Editor!.AddKind.Should().Be(ContentItemKind.Subtask);
        accepted.State.Editor!.SelectedIndex.Should().Be(TodoTaskEditorState.FieldCount + 1);
        accepted.State.Editor.Items.Should().SatisfyRespectively(
            item => item.Should().BeOfType<ContentNoteDraft>(),
            item => item.Should().BeOfType<ContentSubtaskDraft>()
                .Which.Title.Should().Be("N"),
            item => item.Should().BeOfType<ContentSubtaskDraft>()
                .Which.Title.Should().Be("Existing child"));
    }

    [Fact]
    public void Reduce_reports_when_completion_is_used_on_a_note()
    {
        var identity = new TodoIdentity("/alpha.md", 1);
        var todo = new TodoItem(
            1, false, null, "Parent", null, [], null, null, string.Empty,
            [new TodoNote(2, "Note")], []);
        var view = SelectedView(identity, todo);
        var opened = reducer.Reduce(BrowserState.Initial, Key('E'), Configuration, view);
        var noteSelected = opened.State with
        {
            Editor = opened.State.Editor! with { SelectedIndex = TodoTaskEditorState.FieldCount }
        };

        var toggled = reducer.Reduce(noteSelected, Key(ConsoleKey.Spacebar), Configuration, view);

        toggled.State.Editor!.Error.Should().Be("Only subtasks can be completed.");
        toggled.State.Editor.Items.Should().ContainSingle()
            .Which.Should().BeOfType<ContentNoteDraft>();
    }

    [Fact]
    public void Reduce_opens_the_selected_todo_in_the_external_editor()
    {
        var identity = new TodoIdentity("/alpha.md", 7);
        var view = SelectedView(identity);

        var direct = reducer.Reduce(
            BrowserState.Initial,
            Key(ConsoleKey.E, control: true),
            Configuration,
            view);
        var palette = reducer.ReduceAction(BrowserState.Initial, BrowserAction.EditExternal, view);

        direct.Operation.Should().Be(BrowserOperation.EditExternal);
        direct.ProjectPath.Should().Be(identity.ProjectPath);
        direct.TodoIdentity.Should().Be(identity);
        palette.Operation.Should().Be(BrowserOperation.EditExternal);
    }

    [Fact]
    public void Reduce_requires_a_selected_todo_for_external_editing()
    {
        var result = reducer.Reduce(
            BrowserState.Initial,
            Key(ConsoleKey.E, control: true),
            Configuration,
            EmptyView());

        result.Operation.Should().Be(BrowserOperation.None);
        result.State.Error.Should().Be("Select a todo to edit externally.");
    }

    [Fact]
    public void Reduce_confirms_removing_a_subtask_with_descendants()
    {
        var identity = new TodoIdentity("/alpha.md", 1);
        var grandchild = new TodoItem(4, false, null, "Grandchild", null, [], null, null, string.Empty, [], []);
        var child = new TodoItem(
            2, false, null, "Child", null, [], null, null, string.Empty,
            [new TodoNote(3, "note")], [grandchild]);
        var todo = new TodoItem(1, false, null, "Parent", null, [], null, null, string.Empty, [], [child]);
        var view = SelectedView(identity, todo);
        var opened = reducer.Reduce(BrowserState.Initial, Key('E'), Configuration, view);
        var subtaskSelected = opened.State with
        {
            Editor = opened.State.Editor! with { SelectedIndex = TodoTaskEditorState.FieldCount }
        };

        var requested = reducer.Reduce(subtaskSelected, Key('d'), Configuration, view);
        var confirmed = reducer.Reduce(requested.State, Key('l'), Configuration, view);

        requested.State.Editor!.Mode.Should().Be(TodoTaskEditorMode.ConfirmRemoval);
        confirmed.State.Editor!.Items.Should().BeEmpty();
    }

    [Fact]
    public void Reduce_hides_and_restores_details_with_focus_changes()
    {
        var hidden = reducer.Reduce(
            BrowserState.Initial with { Focus = BrowserFocus.Details },
            Key('v'),
            Configuration,
            EmptyView());
        var shown = reducer.Reduce(hidden.State, Key('v'), Configuration, EmptyView());

        hidden.State.ShowDetails.Should().BeFalse();
        hidden.State.Focus.Should().Be(BrowserFocus.Todos);
        shown.State.ShowDetails.Should().BeTrue();
        shown.State.Focus.Should().Be(BrowserFocus.Details);
    }

    [Fact]
    public void Reduce_skips_hidden_details_but_opening_a_todo_restores_them()
    {
        var state = BrowserState.Initial with
        {
            Focus = BrowserFocus.Todos,
            ShowDetails = false
        };

        var cycled = reducer.Reduce(state, Key(ConsoleKey.Tab), Configuration, EmptyView());
        var opened = reducer.Reduce(state, Key('l'), Configuration, EmptyView());

        cycled.State.Focus.Should().Be(BrowserFocus.Projects);
        cycled.State.ShowDetails.Should().BeFalse();
        opened.State.Focus.Should().Be(BrowserFocus.Details);
        opened.State.ShowDetails.Should().BeTrue();
    }

    [Fact]
    public void ReduceAction_toggles_details_for_command_palette_execution()
    {
        var result = reducer.ReduceAction(
            BrowserState.Initial,
            BrowserAction.ToggleDetails,
            EmptyView());

        result.State.ShowDetails.Should().BeFalse();
    }

    private static BrowserView EmptyView() => new(
        BrowserState.Initial,
        [new ProjectRow("All", 0, null, null, true)],
        ImmutableArray<TodoRow>.Empty,
        null,
        "All",
        null,
        null,
        "No projects found");

    private static BrowserView NavigationView() => new(
        BrowserState.Initial,
        [
            new ProjectRow("All", 0, null, null, true),
            new ProjectRow("Alpha", 0, null, null, false)
        ],
        ImmutableArray<TodoRow>.Empty,
        null,
        "All",
        null,
        null,
        "No todos");

    private static BrowserView TodoNavigationView(int count)
    {
        var todos = Enumerable.Range(1, count)
            .Select(index => new TodoItem(
                index, false, null, $"Todo {index}", null, [], null, null, string.Empty, [], []))
            .ToArray();
        return new BrowserView(
            BrowserState.Initial with { Focus = BrowserFocus.Todos },
            [new ProjectRow("All", count, null, null, true)],
            [.. todos.Select((todo, index) => new TodoRow(null, todo, [], index == 0))],
            todos[0],
            "All",
            null,
            null,
            string.Empty);
    }

    private static BrowserView SelectedView(TodoIdentity identity)
    {
        var todo = new WolfTodo.Core.Features.ProjectBrowser.TodoItem(
            identity.SourceLine,
            false,
            null,
            "Selected",
            null,
            [],
            null,
            null,
            string.Empty,
            [],
            []);
        return SelectedView(identity, todo);
    }

    private static BrowserView SelectedView(TodoIdentity identity, TodoItem todo)
    {
        return new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, [], true, identity)],
            todo,
            "All",
            null,
            null,
            string.Empty);
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, bool control = false) =>
        new('\0', key, false, false, control);

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem2, false, false, false);
}
