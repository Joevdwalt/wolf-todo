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
