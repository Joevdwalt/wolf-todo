using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class TaskLinkWorkflowTests
{
    private readonly TaskLinkWorkflow workflow = new();
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");
    private static readonly TodoItem Child = new(4, false, null, "Child", null, [], null, null, "", [], []);
    private static readonly TodoProject Project = new("Work", "/todos/work.md",
        [new TodoItem(3, true, null, "Parent", null, [], null, null, "", [], [Child])]);
    private static readonly ProjectCatalog Catalog = new([Project], []);
    private static ApplicationState State => new(new TabHostState(new TabId("planner")),
        BrowserState.Initial with { FilterText = "unrelated", Sort = new TodoSort(TodoSortProperty.Name, TodoSortDirection.Descending) });

    [Fact]
    public void Open_exits_focus_and_reveals_a_descendant_of_a_completed_parent_without_losing_sort()
    {
        var state = State with { FocusedTask = FocusedTaskState.Create(new TodoIdentity(Project.Path, 3), Project.Todos[0]) };
        var opened = workflow.Open(state, Catalog, TaskLinkCode.Generate(Project.Path, 4), 2);
        opened.Tabs.ActiveTab.Should().Be(new TabId("todos"));
        opened.FocusedTask.Should().BeNull();
        opened.Browser.ProjectIndex.Should().Be(4);
        opened.Browser.ShowCompleted.Should().BeTrue();
        opened.Browser.Sort.Should().Be(state.Browser.Sort);
        opened.Browser.FilterText.Should().BeEmpty();
        opened.Browser.Focus.Should().Be(BrowserFocus.Todos);
        opened.Browser.PendingTodoSelection.Should().Be(new TodoIdentity(Project.Path, 4));
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("wt1-00000000")]
    public void Open_failure_preserves_navigation_and_focus(string code)
    {
        var state = State with { FocusedTask = FocusedTaskState.Create(new TodoIdentity(Project.Path, 3), Project.Todos[0]) };
        var result = workflow.Open(state, Catalog, code, 0);
        result.Browser.Should().Be(state.Browser);
        result.Planner.Should().Be(state.Planner);
        result.Tabs.Should().Be(state.Tabs);
        result.FocusedTask.Should().Be(state.FocusedTask);
        result.Command.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Generate_uses_the_highlighted_subtask_and_preserves_focus_while_protecting_the_code()
    {
        var focus = FocusedTaskState.Create(new TodoIdentity(Project.Path, 3), Project.Todos[0]) with
            { SelectedIdentity = new TodoIdentity(Project.Path, 4) };
        var state = State with { FocusedTask = focus };
        var view = new FocusedTaskPresenter().CreateView(Catalog, focus)!;
        var generated = workflow.Generate(state, Catalog, null, null, view);
        generated.TaskLinkPanel!.Input.Text.Should().Be(TaskLinkCode.Generate(Project.Path, 4));
        generated.TaskLinkPanel.Context.Should().Contain("Work").And.Contain("Line 4").And.Contain("current location");
        generated.FocusedTask.Should().Be(focus);
        var ignored = workflow.ReducePanel(generated, new ConsoleKeyInfo('z', ConsoleKey.Z, false, false, false), Bindings, Catalog, 0);
        ignored.Should().Be(generated);
        workflow.ReducePanel(generated, Key(ConsoleKey.Escape), Bindings, Catalog, 0).TaskLinkPanel.Should().BeNull();
    }

    [Fact]
    public void Generate_uses_browser_selection_and_rejects_empty_or_calendar_selection()
    {
        var browserState = BrowserState.Initial with { ShowCompleted = true, PendingTodoSelection = new TodoIdentity(Project.Path, 4) };
        var view = new ProjectBrowserPresenter().CreateView(Catalog, browserState);
        var generated = workflow.Generate(State with { Tabs = new TabHostState(new TabId("todos")) }, Catalog, view, null, null);
        generated.TaskLinkPanel!.Input.Text.Should().Be(TaskLinkCode.Generate(Project.Path, 4));
        workflow.Generate(State, Catalog, null, null, null).Command.Error.Should().Contain("Select a Markdown task");
    }

    [Fact]
    public void Prompt_can_be_cancelled_or_submitted_through_the_textbox()
    {
        var prompt = workflow.Prompt(State);
        workflow.ReducePanel(prompt, Key(ConsoleKey.Escape), Bindings, Catalog, 0).Browser.Should().Be(State.Browser);
        var typed = prompt with { TaskLinkPanel = prompt.TaskLinkPanel! with
            { Input = TextBox.Create("OPEN TASK LINK", true, TaskLinkCode.Generate(Project.Path, 4), true) } };
        var opened = workflow.ReducePanel(typed, Key(ConsoleKey.Enter), Bindings, Catalog, 0);
        opened.TaskLinkPanel.Should().BeNull();
        opened.Browser.PendingTodoSelection.Should().Be(new TodoIdentity(Project.Path, 4));
    }

    [Fact]
    public void Generate_uses_the_planner_task_even_when_multiple_items_share_a_slot()
    {
        var identity = new TodoIdentity(Project.Path, 4);
        var assignment = new PlannerAssignment(identity, Project.Title, Project.Path, Child);
        var time = new TimeOnly(9, 0);
        var item = new PlannerTimelineItemView(PlannerItemType.Task, "task", Child.Title, time, time,
            PlannerTimeShape.Instant, PlannerIntervalState.Instant, false, true, assignment);
        var planner = new PlannerView(PlannerState.CreateInitial(new DateOnly(2026, 9, 14)),
            [new PlannerSlotView(time, [assignment, assignment], true) with { Items = [item] }], [], []);
        var generated = workflow.Generate(State, Catalog, null, planner, null);
        generated.TaskLinkPanel!.Input.Text.Should().Be(TaskLinkCode.Generate(Project.Path, 4));
        generated.Planner.Should().Be(State.Planner);
        var items = new ApplicationActionCatalog().Create(false, null, planner, Bindings);
        items.Single(action => action.Action == ApplicationActionId.GenerateTaskLink).IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Open_collision_reports_the_ambiguity_without_navigation()
    {
        var project = new TodoProject("Work", "/todos/work.md", [Child with { SourceLine = 66654 }, Child with { SourceLine = 82819 }]);
        var result = workflow.Open(State, new ProjectCatalog([project], []), TaskLinkCode.Generate(project.Path, 82819), 0);
        result.Browser.Should().Be(State.Browser);
        result.Tabs.Should().Be(State.Tabs);
        result.Command.Error.Should().Contain("multiple locations");
    }

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);
}
