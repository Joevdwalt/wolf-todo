using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class ProjectBrowserPresenterTests
{
    private readonly ProjectBrowserPresenter presenter = new();

    [Fact]
    public void CreateView_groups_all_projects_and_hides_completed_todos()
    {
        var catalog = new ProjectCatalog(
            [
                Project("Alpha", Todo("Open"), Todo("Done", completed: true)),
                Project("Beta", Todo("Another"))
            ],
            []);

        var result = presenter.CreateView(catalog, BrowserState.Initial);

        result.Projects[0].Title.Should().Be("All");
        result.Projects[0].ActiveCount.Should().Be(2);
        result.Todos.Where(row => row.Heading is not null).Select(row => row.Heading)
            .Should().Equal("Alpha", "Beta");
        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal("Open", "Another");
    }

    [Fact]
    public void CreateView_includes_completed_todos_after_open_todos_when_enabled()
    {
        var catalog = new ProjectCatalog(
            [Project("Alpha", Todo("Done", completed: true), Todo("Open"))],
            []);
        var state = BrowserState.Initial with { ShowCompleted = true };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal("Open", "Done");
    }

    [Fact]
    public void CreateView_exposes_selected_source_error_as_a_diagnostic()
    {
        var catalog = new ProjectCatalog([], [new ProjectSourceError("missing", "/missing", "not found")]);
        var state = BrowserState.Initial with { ProjectIndex = 1 };

        var result = presenter.CreateView(catalog, state);

        result.Diagnostic.Should().Be("not found");
        result.SelectedProjectPath.Should().Be("/missing");
    }

    [Theory]
    [InlineData("RENEWAL", "Contract renewal")]
    [InlineData("abc-123", "Reference match")]
    [InlineData("#NOW", "Tag match")]
    [InlineData("contracts", "Section match")]
    public void CreateView_filters_todos_by_supported_metadata_case_insensitively(
        string filter,
        string expectedTitle)
    {
        var catalog = new ProjectCatalog(
            [Project(
                "Alpha",
                Todo("Title match") with { Title = "Contract renewal" },
                Todo("Reference match") with { ExternalReference = "ABC-123" },
                Todo("Tag match") with { Tags = ["now"] },
                Todo("Section match") with { SectionPath = "Client / Contracts" },
                Todo("No match"))],
            []);
        var state = BrowserState.Initial with { FilterText = filter };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal(expectedTitle);
    }

    [Fact]
    public void CreateView_uses_the_filter_draft_live_while_filter_mode_is_active()
    {
        var catalog = new ProjectCatalog(
            [Project("Alpha", Todo("Renew contract"), Todo("Prepare invoice"))],
            []);
        var state = BrowserState.Initial with
        {
            IsFilterMode = true,
            FilterText = "invoice",
            FilterDraft = "renew"
        };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal("Renew contract");
    }

    [Fact]
    public void CreateView_filters_only_the_selected_project_and_keeps_project_counts_unfiltered()
    {
        var catalog = new ProjectCatalog(
            [
                Project("Alpha", Todo("Alpha match"), Todo("Alpha other")),
                Project("Beta", Todo("Beta match"), Todo("Beta other"))
            ],
            []);
        var state = BrowserState.Initial with { ProjectIndex = 2, FilterText = "match" };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal("Beta match");
        result.Projects[2].ActiveCount.Should().Be(2);
    }

    [Fact]
    public void CreateView_omits_nonmatching_project_and_section_headings_in_all()
    {
        var catalog = new ProjectCatalog(
            [
                Project("Alpha", Todo("Match") with { SectionPath = "Included" }),
                Project("Beta", Todo("Other") with { SectionPath = "Excluded" })
            ],
            []);
        var state = BrowserState.Initial with { FilterText = "match" };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Heading is not null).Select(row => row.Heading)
            .Should().Equal("Alpha", "Included");
    }

    [Fact]
    public void CreateView_shows_a_matching_subtask_without_its_nonmatching_parent()
    {
        var child = Todo("Matching child");
        var parent = Todo("Unrelated parent") with { Subtasks = [child] };
        var catalog = new ProjectCatalog([Project("Alpha", parent)], []);
        var state = BrowserState.Initial with { FilterText = "matching" };

        var result = presenter.CreateView(catalog, state);
        var todoRows = result.Todos.Where(row => row.Todo is not null).ToArray();

        todoRows.Should().ContainSingle();
        todoRows[0].Todo.Should().BeSameAs(child);
        todoRows[0].Depth.Should().Be(1);
    }

    [Fact]
    public void CreateView_applies_completed_visibility_before_filtering()
    {
        var catalog = new ProjectCatalog([Project("Alpha", Todo("Matching done", completed: true))], []);
        var state = BrowserState.Initial with { FilterText = "matching" };

        var hidden = presenter.CreateView(catalog, state);
        var shown = presenter.CreateView(catalog, state with { ShowCompleted = true });

        hidden.Todos.Where(row => row.Todo is not null).Should().BeEmpty();
        hidden.EmptyMessage.Should().Be("No todos match /matching");
        shown.Todos.Where(row => row.Todo is not null).Should().ContainSingle();
    }

    [Theory]
    [InlineData(TodoSortDirection.Ascending, "Task 2", "Task 10")]
    [InlineData(TodoSortDirection.Descending, "Task 10", "Task 2")]
    public void CreateView_sorts_names_naturally(
        TodoSortDirection direction,
        string first,
        string second)
    {
        var catalog = new ProjectCatalog(
            [Project("Alpha", Todo("Task 10") with { SourceLine = 1 }, Todo("Task 2") with { SourceLine = 2 })],
            []);
        var state = BrowserState.Initial with
        {
            ProjectIndex = 1,
            Sort = new TodoSort(TodoSortProperty.Name, direction)
        };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal(first, second);
    }

    [Theory]
    [InlineData(TodoSortDirection.Ascending, "Early", "Late")]
    [InlineData(TodoSortDirection.Descending, "Late", "Early")]
    public void CreateView_sorts_start_dates_and_keeps_missing_dates_last(
        TodoSortDirection direction,
        string first,
        string second)
    {
        var catalog = new ProjectCatalog(
            [Project(
                "Alpha",
                Todo("Missing") with { SourceLine = 1 },
                Todo("Late") with { SourceLine = 2, StartDate = new DateOnly(2026, 8, 1) },
                Todo("Early") with { SourceLine = 3, StartDate = new DateOnly(2026, 7, 1) })],
            []);
        var state = BrowserState.Initial with
        {
            ProjectIndex = 1,
            Sort = new TodoSort(TodoSortProperty.StartDate, direction)
        };

        var result = presenter.CreateView(catalog, state);
        var titles = result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title);

        titles.Should().Equal(first, second, "Missing");
    }

    [Theory]
    [InlineData(TodoSortDirection.Ascending, "Beta set", "Gamma set")]
    [InlineData(TodoSortDirection.Descending, "Gamma set", "Beta set")]
    public void CreateView_sorts_by_normalized_tag_sets_and_keeps_untagged_todos_last(
        TodoSortDirection direction,
        string first,
        string second)
    {
        var catalog = new ProjectCatalog(
            [Project(
                "Alpha",
                Todo("Beta set") with { SourceLine = 1, Tags = ["beta", "alpha", "ALPHA"] },
                Todo("Gamma set") with { SourceLine = 2, Tags = ["gamma", "alpha"] },
                Todo("None") with { SourceLine = 3 })],
            []);
        var state = BrowserState.Initial with
        {
            ProjectIndex = 1,
            Sort = new TodoSort(TodoSortProperty.Tags, direction)
        };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal(first, second, "None");
    }

    [Theory]
    [InlineData(
        TodoSortDirection.Ascending,
        "Lowest,Low,Medium,High,Highest,None")]
    [InlineData(
        TodoSortDirection.Descending,
        "Highest,High,Medium,Low,Lowest,None")]
    public void CreateView_sorts_priorities_and_keeps_unprioritized_todos_last(
        TodoSortDirection direction,
        string expectedOrder)
    {
        var catalog = new ProjectCatalog(
            [Project(
                "Alpha",
                Todo("None") with { SourceLine = 1 },
                Todo("High") with { SourceLine = 2, Priority = TodoPriority.High },
                Todo("Lowest") with { SourceLine = 3, Priority = TodoPriority.Lowest },
                Todo("Highest") with { SourceLine = 4, Priority = TodoPriority.Highest },
                Todo("Medium") with { SourceLine = 5, Priority = TodoPriority.Medium },
                Todo("Low") with { SourceLine = 6, Priority = TodoPriority.Low })],
            []);
        var state = BrowserState.Initial with
        {
            ProjectIndex = 1,
            Sort = new TodoSort(TodoSortProperty.Priority, direction)
        };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal(expectedOrder.Split(','));
    }

    [Theory]
    [InlineData(TodoSortDirection.Ascending, "Two", "Ten")]
    [InlineData(TodoSortDirection.Descending, "Ten", "Two")]
    public void CreateView_sorts_all_project_groups_by_markdown_filename(
        TodoSortDirection direction,
        string first,
        string second)
    {
        var catalog = new ProjectCatalog(
            [
                new TodoProject("Ten", "/projects/work10.md", [Todo("Ten task")]),
                new TodoProject("Two", "/projects/work2.md", [Todo("Two task")])
            ],
            []);
        var state = BrowserState.Initial with
        {
            Sort = new TodoSort(TodoSortProperty.File, direction)
        };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Heading is not null).Select(row => row.Heading)
            .Should().Equal(first, second);
    }

    [Fact]
    public void CreateView_keeps_subtasks_attached_to_their_sorted_parent_block()
    {
        var child = Todo("A child") with { SourceLine = 2 };
        var parent = Todo("Z parent") with { SourceLine = 1, Subtasks = [child] };
        var sibling = Todo("M sibling") with { SourceLine = 3 };
        var catalog = new ProjectCatalog([Project("Alpha", parent, sibling)], []);
        var state = BrowserState.Initial with
        {
            ProjectIndex = 1,
            Sort = new TodoSort(TodoSortProperty.Name, TodoSortDirection.Ascending)
        };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal("M sibling", "Z parent", "A child");
    }

    [Fact]
    public void CreateView_keeps_open_todos_before_completed_todos_when_sorting()
    {
        var catalog = new ProjectCatalog(
            [Project("Alpha", Todo("A completed", completed: true), Todo("Z open"))],
            []);
        var state = BrowserState.Initial with
        {
            ProjectIndex = 1,
            ShowCompleted = true,
            Sort = new TodoSort(TodoSortProperty.Name, TodoSortDirection.Ascending)
        };

        var result = presenter.CreateView(catalog, state);

        result.Todos.Where(row => row.Todo is not null).Select(row => row.Todo!.Title)
            .Should().Equal("Z open", "A completed");
    }

    [Fact]
    public void CreateView_restores_a_pending_todo_selection_after_sorting()
    {
        var catalog = new ProjectCatalog(
            [Project("Alpha", Todo("Zulu") with { SourceLine = 1 }, Todo("Alpha") with { SourceLine = 2 })],
            []);
        var state = BrowserState.Initial with
        {
            ProjectIndex = 1,
            TodoIndex = 0,
            Sort = new TodoSort(TodoSortProperty.Name, TodoSortDirection.Ascending),
            PendingTodoSelection = new TodoIdentity("/Alpha.md", 1)
        };

        var result = presenter.CreateView(catalog, state);

        result.SelectedTodo!.Title.Should().Be("Zulu");
        result.State.TodoIndex.Should().Be(1);
        result.State.PendingTodoSelection.Should().BeNull();
    }

    private static TodoProject Project(string title, params TodoItem[] todos) => new(title, $"/{title}.md", [.. todos]);

    private static TodoItem Todo(string title, bool completed = false) => new(
        1,
        completed,
        null,
        title,
        null,
        [],
        null,
        null,
        string.Empty,
        [],
        []);
}
