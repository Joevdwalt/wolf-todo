using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class FocusedTaskWorkflow(
    ProjectCatalogLoader catalogLoader,
    ITerminalUi terminalUi,
    IExternalEditorLauncher? externalEditorLauncher)
{
    public (ApplicationState State, ProjectCatalog Catalog) ApplyTransition(
        ApplicationState state,
        FocusedTaskTransition transition,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? mutationService)
    {
        if (transition.Operation == FocusedTaskOperation.Exit)
        {
            return (state with { FocusedTask = null }, catalog);
        }

        state = state with { FocusedTask = transition.State };
        if (transition.Operation == FocusedTaskOperation.None)
        {
            return (state, catalog);
        }

        if (transition.Operation == FocusedTaskOperation.EditExternal)
        {
            return ApplyExternalEdit(state, transition, catalog, configuration);
        }

        if (mutationService is null || transition.Identity is null || transition.ExpectedTodo is null)
        {
            return (Failure(state, "Todo writing is unavailable."), catalog);
        }

        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var result = transition.Operation switch
        {
            FocusedTaskOperation.Update when transition.Update is not null => mutationService.UpdateTask(
                transition.Identity.ProjectPath,
                transition.ExpectedTodo,
                transition.Update),
            FocusedTaskOperation.ToggleCompleted => mutationService.SetCompleted(
                transition.Identity.ProjectPath,
                transition.ExpectedTodo,
                !transition.ExpectedTodo.IsCompleted),
            _ => TodoMutationResult.Failure("The requested focused-task change is invalid.")
        };
        if (!result.Succeeded)
        {
            return (Failure(state, result.Error ?? "The focused task could not be updated."), catalog);
        }

        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var focus = state.FocusedTask!;
        var updatedIdentity = result.SourceLine is { } line
            ? new TodoIdentity(transition.Identity.ProjectPath, line)
            : transition.Identity;
        var rootIdentity = transition.Identity == focus.RootIdentity ? updatedIdentity : focus.RootIdentity;
        var rootSnapshot = FindTodo(catalog, rootIdentity);
        if (rootSnapshot is null)
        {
            return (CloseWithMessage(state, "Focused task is no longer available."), catalog);
        }
        return (state with
        {
            FocusedTask = focus with
            {
                RootIdentity = rootIdentity,
                RootSnapshot = rootSnapshot,
                SelectedIdentity = updatedIdentity,
                Editor = null,
                Error = null,
                StatusMessage = "Todo update saved."
            }
        }, catalog);
    }

    public (ApplicationState State, ProjectCatalog Catalog) MoveSelectedToProject(
        ApplicationState state,
        FocusedTaskView view,
        string? targetTitle,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? mutationService)
    {
        var target = catalog.Projects.FirstOrDefault(project =>
            string.Equals(project.Title, targetTitle, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return (Failure(state, $"Project not found: {targetTitle}"), catalog);
        }

        if (mutationService is null)
        {
            return (Failure(state, "Todo writing is unavailable."), catalog);
        }

        var selected = view.SelectedItem;
        var result = mutationService.Move(selected.Identity.ProjectPath, target.Path, selected.Todo);
        if (!result.Succeeded || result.SourceLine is null)
        {
            return (Failure(state, result.Error ?? "The focused task could not be moved."), catalog);
        }

        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var identity = new TodoIdentity(target.Path, result.SourceLine.Value);
        var moved = FindTodo(catalog, identity);
        if (moved is null)
        {
            return (CloseWithMessage(state, "Focused task is no longer available."), catalog);
        }

        return (state with
        {
            FocusedTask = FocusedTaskState.Create(identity, moved) with { StatusMessage = "Todo moved." }
        }, catalog);
    }

    public static ApplicationState CloseWithMessage(ApplicationState state, string message) =>
        state.Tabs.ActiveTab.Value == "todos"
            ? state with
            {
                FocusedTask = null,
                Browser = state.Browser with { StatusMessage = message, Error = null }
            }
            : state with
            {
                FocusedTask = null,
                Planner = state.Planner with { Error = message }
            };

    private (ApplicationState State, ProjectCatalog Catalog) ApplyExternalEdit(
        ApplicationState state,
        FocusedTaskTransition transition,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration)
    {
        if (externalEditorLauncher is null || transition.Identity is null)
        {
            return (Failure(state, "External editing is unavailable."), catalog);
        }

        ExternalEditorResult result;
        terminalUi.SuspendForExternalProcess();
        try
        {
            result = externalEditorLauncher.Open(
                transition.Identity.ProjectPath,
                transition.Identity.SourceLine);
        }
        finally
        {
            terminalUi.ResumeAfterExternalProcess();
        }

        if (result.Started)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
        }

        return result.Error is null
            ? (state, catalog)
            : (Failure(state, result.Error), catalog);
    }

    private static ApplicationState Failure(ApplicationState state, string error) => state with
    {
        FocusedTask = state.FocusedTask! with
        {
            Error = error,
            StatusMessage = null,
            Editor = state.FocusedTask.Editor is null
                ? null
                : state.FocusedTask.Editor with { Error = error }
        }
    };

    private static TodoItem? FindTodo(ProjectCatalog catalog, TodoIdentity identity)
    {
        var project = catalog.Projects.FirstOrDefault(candidate => candidate.Path == identity.ProjectPath);
        return project is null
            ? null
            : Flatten(project.Todos).FirstOrDefault(todo => todo.SourceLine == identity.SourceLine);
    }

    private static IEnumerable<TodoItem> Flatten(IEnumerable<TodoItem> todos)
    {
        foreach (var todo in todos)
        {
            yield return todo;
            foreach (var child in Flatten(todo.Subtasks)) yield return child;
        }
    }
}
