using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ApplicationShell.ExternalEditing;
using WolfTodo.Tui.Features.ApplicationShell.Runtime;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class BrowserTransitionExecutor(
    ProjectCatalogLoader catalogLoader,
    Func<DateOnly> todayProvider,
    ProjectTodoMutationService? mutationService,
    ExternalTodoEditorExecutor externalEditor)
{
    public ApplicationRuntime Apply(
        ApplicationRuntime runtime,
        BrowserTransition transition)
    {
        runtime = runtime with { State = runtime.State with { Browser = transition.State } };
        if (transition.Operation == BrowserOperation.None)
        {
            return runtime;
        }

        return transition.Operation == BrowserOperation.EditExternal
            ? ApplyExternalEdit(runtime, transition)
            : ApplyMutation(runtime, transition);
    }

    public ApplicationRuntime MoveTodoToProject(
        ApplicationRuntime runtime,
        BrowserView? view,
        string? targetTitle)
    {
        if (runtime.State.Tabs.ActiveTab != ApplicationTabs.Todos ||
            view?.SelectedTodoIdentity is not { } identity)
        {
            return Failure(runtime, "Select a todo in the Todos tab before moving it.");
        }

        var target = runtime.Catalog.Projects.FirstOrDefault(project =>
            string.Equals(project.Title, targetTitle, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return Failure(runtime, $"Project not found: {targetTitle}");
        }

        var source = runtime.Catalog.Projects.FirstOrDefault(project => project.Path == identity.ProjectPath);
        var todo = source is null ? null : TodoCatalogLookup.Find(source.Todos, identity.SourceLine);
        if (todo is null || mutationService is null)
        {
            return Failure(runtime, "The selected todo cannot be moved.");
        }

        var result = mutationService.Move(source!.Path, target.Path, todo);
        return result.Succeeded
            ? SelectMovedProject(Reload(runtime), target.Path)
            : Failure(runtime, result.Error ?? "The selected todo cannot be moved.");
    }

    private ApplicationRuntime ApplyMutation(
        ApplicationRuntime runtime,
        BrowserTransition transition)
    {
        var expectedCatalog = runtime.Catalog;
        runtime = Reload(runtime);
        var result = ExecuteMutation(transition, expectedCatalog);
        var browser = runtime.State.Browser with
        {
            Error = result.Error,
            Editor = result.Succeeded
                ? null
                : runtime.State.Browser.Editor is null
                    ? null
                    : runtime.State.Browser.Editor with { Error = result.Error },
            PendingTodoSelection = PendingSelection(transition, result)
        };
        var updated = runtime with { State = runtime.State with { Browser = browser } };
        return result.Succeeded ? Reload(updated) : updated;
    }

    private TodoMutationResult ExecuteMutation(
        BrowserTransition transition,
        ProjectCatalog expectedCatalog)
    {
        if (mutationService is null || transition.ProjectPath is null)
        {
            return TodoMutationResult.Failure("Todo writing is unavailable.");
        }

        if (transition.Operation == BrowserOperation.Create && transition.Update is not null)
        {
            return mutationService.Create(transition.ProjectPath, transition.Update);
        }

        if (transition.Operation == BrowserOperation.RollProjectToday)
        {
            var project = expectedCatalog.Projects.FirstOrDefault(
                candidate => candidate.Path == transition.ProjectPath);
            return project is null
                ? TodoMutationResult.Failure("The selected project cannot be found.")
                : mutationService.RollOverdueToDate(transition.ProjectPath, project, todayProvider());
        }

        var expected = TodoCatalogLookup.Find(expectedCatalog, transition.TodoIdentity);
        if (expected is null)
        {
            return TodoMutationResult.Failure("The selected todo cannot be found.");
        }

        return transition.Operation switch
        {
            BrowserOperation.Update when transition.Update is not null =>
                mutationService.UpdateTask(transition.ProjectPath, expected, transition.Update),
            BrowserOperation.ToggleCompleted =>
                mutationService.SetCompleted(transition.ProjectPath, expected, !expected.IsCompleted),
            _ => TodoMutationResult.Failure("The requested todo change is invalid.")
        };
    }

    private ApplicationRuntime ApplyExternalEdit(
        ApplicationRuntime runtime,
        BrowserTransition transition)
    {
        var result = externalEditor.Open(transition.ProjectPath, transition.TodoIdentity);
        var updated = runtime with
        {
            State = runtime.State with
            {
                Browser = runtime.State.Browser with
                {
                    PendingTodoSelection = null,
                    Error = result.Error
                }
            }
        };
        return result.Started ? Reload(updated) : updated;
    }

    private ApplicationRuntime Reload(ApplicationRuntime runtime) =>
        runtime with { Catalog = catalogLoader.Load(runtime.Configuration.ProjectFiles) };

    private static TodoIdentity? PendingSelection(
        BrowserTransition transition,
        TodoMutationResult result) =>
        result.Succeeded && result.SourceLine is not null && transition.ProjectPath is not null
            ? new TodoIdentity(transition.ProjectPath, result.SourceLine.Value)
            : result.Succeeded && transition.Operation == BrowserOperation.RollProjectToday
                ? transition.TodoIdentity
                : null;

    private static ApplicationRuntime Failure(ApplicationRuntime runtime, string error) =>
        runtime with
        {
            State = runtime.State with
            {
                Browser = runtime.State.Browser with { Error = error }
            }
        };

    private static ApplicationRuntime SelectMovedProject(ApplicationRuntime runtime, string targetPath)
    {
        var index = runtime.Catalog.Projects
            .Select((project, candidateIndex) => (project, candidateIndex))
            .First(candidate => candidate.project.Path == targetPath)
            .candidateIndex;
        return runtime with
        {
            State = runtime.State with
            {
                Browser = runtime.State.Browser with
                {
                    Focus = BrowserFocus.Todos,
                    ProjectIndex = Math.Max(0, index),
                    TodoIndex = 0,
                    PendingTodoSelection = null,
                    Error = null
                }
            }
        };
    }
}
