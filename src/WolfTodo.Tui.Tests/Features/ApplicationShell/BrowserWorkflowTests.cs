using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class BrowserWorkflowTests
{
    private static readonly TodoIdentity Identity = new("/work.md", 12);

    [Theory]
    [InlineData(true, null)]
    [InlineData(true, "editor exited")]
    [InlineData(false, "editor unavailable")]
    public void ApplyExternalEdit_reloads_and_clears_transient_state_only_when_editor_started(bool started, string? error)
    {
        var fs = new BrowserWorkflowTestFileSystem();
        var reader = new MarkdownTodoProjectReader();
        var loader = new ProjectCatalogLoader(new MarkdownTodoProjectRepository(fs, reader));
        var terminal = new BrowserWorkflowTestTerminal();
        var editor = new BrowserWorkflowTestEditor(new ExternalEditorResult(started, error),
            () => fs.WriteAllTextAtomically("/work.md", "# Work\n\n- [ ] Changed\n"));
        var workflow = new BrowserWorkflow(loader, editor, terminal, () => new DateOnly(2026, 1, 1), new ProjectBrowserPresenter());
        var config = new ApplicationConfiguration(["/work.md"], ":q");
        var catalog = loader.Load(config.ProjectFiles);
        var identity = new TodoIdentity("/work.md", 3);
        var browser = BrowserState.Initial with { MarkedTodos = [identity], CollapsedTodos = [identity], PendingTodoSelection = identity };
        var transition = new BrowserTransition(browser, BrowserOperation.EditExternal, identity.ProjectPath, identity);
        var state = new ApplicationState(new TabHostState(new TabId("todos")), BrowserState.Initial);

        var result = workflow.ApplyExternalEdit(state, transition, catalog, config);

        editor.Path.Should().Be("/work.md");
        editor.Line.Should().Be(3);
        terminal.Suspensions.Should().Be(1);
        terminal.Resumptions.Should().Be(1);
        result.Catalog.Projects[0].Todos[0].Title.Should().Be(started ? "Changed" : "First");
        result.State.Browser.Error.Should().Be(error);
        result.State.Browser.MarkedTodos.Contains(identity).Should().Be(!started);
        result.State.Browser.CollapsedTodos.Contains(identity).Should().Be(!started);
        result.State.Browser.PendingTodoSelection.Should().BeNull();
    }

    [Fact]
    public void ApplyExternalEdit_without_launcher_reports_error_and_preserves_selection()
    {
        var fs = new BrowserWorkflowTestFileSystem();
        var loader = new ProjectCatalogLoader(new MarkdownTodoProjectRepository(fs, new MarkdownTodoProjectReader()));
        var workflow = new BrowserWorkflow(loader, null, null!, () => new DateOnly(2026, 1, 1), new ProjectBrowserPresenter());
        var config = new ApplicationConfiguration(["/work.md"], ":q");
        var catalog = loader.Load(config.ProjectFiles);
        var browser = BrowserState.Initial with { PendingTodoSelection = Identity };
        var transition = new BrowserTransition(browser, BrowserOperation.EditExternal, Identity.ProjectPath, Identity);
        var state = new ApplicationState(new TabHostState(new TabId("todos")), BrowserState.Initial);

        var result = workflow.ApplyExternalEdit(state, transition, catalog, config);

        result.State.Browser.Error.Should().Be("External editing is unavailable.");
        result.State.Browser.PendingTodoSelection.Should().Be(Identity);
        result.Catalog.Should().BeSameAs(catalog);
    }

    [Fact]
    public void ApplySingleUpdate_selects_previous_task_after_completion()
    {
        var fs = new BrowserWorkflowTestFileSystem();
        var reader = new MarkdownTodoProjectReader();
        var loader = new ProjectCatalogLoader(new MarkdownTodoProjectRepository(fs, reader));
        var presenter = new ProjectBrowserPresenter();
        var workflow = new BrowserWorkflow(loader, null, null!, () => new DateOnly(2026, 1, 1), presenter);
        var config = new ApplicationConfiguration(["/work.md"], ":q");
        var catalog = loader.Load(config.ProjectFiles);
        var browser = BrowserState.Initial with { TodoIndex = 1 };
        var view = presenter.CreateView(catalog, browser);
        var identity = view.SelectedTodoIdentity!;
        var transition = new BrowserTransition(view.State, BrowserOperation.ToggleCompleted, identity.ProjectPath, identity);
        var state = new ApplicationState(new TabHostState(new TabId("todos")), view.State);

        var result = workflow.ApplySingleUpdate(new BrowserSingleUpdateRequest(
            state, transition, catalog, config, new ProjectTodoMutationService(fs, reader)));

        presenter.CreateView(result.Catalog, result.State.Browser).SelectedTodo!.Title.Should().Be("First");
        fs.Contents.Should().Contain("- [x]");
    }

    [Fact]
    public void ApplySingleUpdate_failed_write_preserves_marked_tasks_and_reports_error()
    {
        var fs = new BrowserWorkflowTestFileSystem { FailWrites = true };
        var reader = new MarkdownTodoProjectReader();
        var loader = new ProjectCatalogLoader(new MarkdownTodoProjectRepository(fs, reader));
        var presenter = new ProjectBrowserPresenter();
        var workflow = new BrowserWorkflow(loader, null, null!, () => new DateOnly(2026, 1, 1), presenter);
        var config = new ApplicationConfiguration(["/work.md"], ":q");
        var catalog = loader.Load(config.ProjectFiles);
        var view = presenter.CreateView(catalog, BrowserState.Initial);
        var identity = view.SelectedTodoIdentity!;
        var browser = view.State with { MarkedTodos = [identity] };
        var transition = new BrowserTransition(browser, BrowserOperation.ToggleCompleted, identity.ProjectPath, identity);
        var state = new ApplicationState(new TabHostState(new TabId("todos")), BrowserState.Initial);

        var result = workflow.ApplySingleUpdate(new BrowserSingleUpdateRequest(
            state, transition, catalog, config, new ProjectTodoMutationService(fs, reader)));

        result.State.Browser.Error.Should().NotBeNullOrEmpty();
        result.State.Browser.MarkedTodos.Should().Contain(identity);
        presenter.CreateView(result.Catalog, result.State.Browser).SelectedTodo!.Title.Should().Be("First");
        fs.Contents.Should().NotContain("- [x]");
    }

    [Fact]
    public void ApplyMutationResult_success_clears_transient_selection_and_restores_result_identity()
    {
        var state = BrowserState.Initial with { MarkedTodos = [Identity], CollapsedTodos = [Identity], Error = "old" };
        var transition = new BrowserTransition(state, BrowserOperation.Update, "/work.md", Identity);

        var actual = BrowserWorkflow.ApplyMutationResult(state, transition, TodoMutationResult.Success(14));

        actual.PendingTodoSelection.Should().Be(new TodoIdentity("/work.md", 14));
        actual.MarkedTodos.Should().BeEmpty();
        actual.CollapsedTodos.Should().BeEmpty();
        actual.Error.Should().BeNull();
        actual.StatusMessage.Should().Be("Todo update saved.");
    }

    [Fact]
    public void ApplyMutationResult_failure_preserves_transient_state_and_reports_error()
    {
        var state = BrowserState.Initial with
        {
            MarkedTodos = [Identity], CollapsedTodos = [Identity], PendingTodoSelection = Identity, StatusMessage = "old"
        };
        var transition = new BrowserTransition(state, BrowserOperation.ToggleCompleted, "/work.md", Identity);

        var actual = BrowserWorkflow.ApplyMutationResult(state, transition, TodoMutationResult.Failure("conflict"));

        actual.MarkedTodos.Should().ContainSingle().Which.Should().Be(Identity);
        actual.CollapsedTodos.Should().ContainSingle().Which.Should().Be(Identity);
        actual.PendingTodoSelection.Should().BeNull();
        actual.StatusMessage.Should().BeNull();
        actual.Error.Should().Be("conflict");
    }

    [Fact]
    public void ApplyMutationResult_rollover_retains_selected_identity_without_a_result_line()
    {
        var state = BrowserState.Initial;
        var transition = new BrowserTransition(state, BrowserOperation.RollProjectToday, "/work.md", Identity);

        BrowserWorkflow.ApplyMutationResult(state, transition, TodoMutationResult.Success())
            .PendingTodoSelection.Should().Be(Identity);
    }
}
