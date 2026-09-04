using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class BrowserWorkflow(
    ProjectCatalogLoader catalogLoader,
    IExternalEditorLauncher? externalEditorLauncher,
    ITerminalUi terminalUi,
    Func<DateOnly> todayProvider)
{
    public (ApplicationState State, ProjectCatalog Catalog) ApplyTransition(
        ApplicationState state,
        BrowserTransition transition,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? mutationService)
    {
        state = state with { Browser = transition.State };
        if (transition.Operation == BrowserOperation.None)
        {
            return (state, catalog);
        }

        if (transition.Operation == BrowserOperation.EditExternal)
        {
            return ApplyExternalEdit(state, transition, catalog, configuration);
        }

        if (transition.Operation == BrowserOperation.BulkUpdate)
        {
            return ApplyBulkUpdate(state, transition, catalog, configuration, mutationService);
        }

        var expectedCatalog = catalog;
        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var result = ApplyOperation(transition, expectedCatalog, mutationService, todayProvider());
        state = state with
        {
            Browser = state.Browser with
            {
                Error = result.Error,
                Editor = result.Succeeded
                    ? null
                    : state.Browser.Editor is null
                        ? null
                        : state.Browser.Editor with { Error = result.Error },
                PendingTodoSelection = result.Succeeded && result.SourceLine is not null &&
                                       transition.ProjectPath is not null
                    ? new TodoIdentity(transition.ProjectPath, result.SourceLine.Value)
                    : result.Succeeded && transition.Operation == BrowserOperation.RollProjectToday
                        ? transition.TodoIdentity
                        : null,
                MarkedTodos = result.Succeeded ? [] : state.Browser.MarkedTodos,
                BulkEditor = result.Succeeded ? null : state.Browser.BulkEditor,
                StatusMessage = result.Succeeded ? "Todo update saved." : null
            }
        };
        if (result.Succeeded)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
        }

        return (state, catalog);
    }

    public (ApplicationState State, ProjectCatalog Catalog) MoveSelectedTodoToProject(
        ApplicationState state,
        BrowserView? view,
        string? targetTitle,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? mutationService,
        bool isTodosTabActive)
    {
        if (!isTodosTabActive || view?.SelectedTodoIdentity is not { } identity)
        {
            return (state with { Browser = state.Browser with { Error = "Select a todo in the Todos tab before moving it." } }, catalog);
        }

        var target = catalog.Projects.FirstOrDefault(project =>
            string.Equals(project.Title, targetTitle, StringComparison.OrdinalIgnoreCase));
        var source = catalog.Projects.FirstOrDefault(project => project.Path == identity.ProjectPath);
        var todo = source is null ? null : Flatten(source.Todos).FirstOrDefault(item => item.SourceLine == identity.SourceLine);
        if (target is null)
        {
            return (state with { Browser = state.Browser with { Error = $"Project not found: {targetTitle}" } }, catalog);
        }
        if (todo is null || mutationService is null)
        {
            return (state with { Browser = state.Browser with { Error = "The selected todo cannot be moved." } }, catalog);
        }

        var result = mutationService.Move(source!.Path, target.Path, todo);
        if (!result.Succeeded)
        {
            return (state with { Browser = state.Browser with { Error = result.Error } }, catalog);
        }

        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var targetIndex = catalog.Projects
            .Select((project, index) => (project, index))
            .FirstOrDefault(candidate => candidate.project.Path == target.Path).index;
        return (state with
        {
            Browser = state.Browser with
            {
                Focus = BrowserFocus.Todos,
                ProjectIndex = Math.Max(0, targetIndex),
                TodoIndex = 0,
                PendingTodoSelection = null,
                Error = null,
                MarkedTodos = [],
                BulkEditor = null,
                StatusMessage = "Todo moved."
            }
        }, catalog);
    }

    public (ApplicationState State, ProjectCatalog Catalog) ArchiveCompletedProject(
        ApplicationState state,
        BrowserView? view,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? mutationService,
        bool isTodosTabActive)
    {
        if (!isTodosTabActive)
        {
            return (state with
            {
                Command = state.Command with { Error = "Open Todos and select a project before archiving." }
            }, catalog);
        }

        var selectedProject = view?.Projects.FirstOrDefault(project => project.IsSelected);
        if (selectedProject?.Kind != ProjectRowKind.Project || selectedProject.Project is null)
        {
            return (state with
            {
                Browser = state.Browser with { Error = "Select a concrete project before archiving completed tasks." }
            }, catalog);
        }

        if (mutationService is null)
        {
            return (state with { Browser = state.Browser with { Error = "Todo writing is unavailable." } }, catalog);
        }

        var result = mutationService.ArchiveCompleted(selectedProject.Project.Path);
        if (!result.Succeeded)
        {
            return (state with { Browser = state.Browser with { Error = result.Error } }, catalog);
        }

        if (result.ArchivedCount == 0)
        {
            return (state with
            {
                Browser = state.Browser with
                {
                    Error = null,
                    StatusMessage = "No completed task trees to archive."
                }
            }, catalog);
        }

        catalog = catalogLoader.Load(configuration.ProjectFiles);
        return (state with
        {
            Browser = state.Browser with
            {
                TodoIndex = 0,
                PendingTodoSelection = null,
                MarkedTodos = [],
                BulkEditor = null,
                Error = null,
                StatusMessage = $"Archived {result.ArchivedCount} task(s) to {Path.GetFileName(result.ArchivePath)}."
            }
        }, catalog);
    }

    private (ApplicationState State, ProjectCatalog Catalog) ApplyBulkUpdate(
        ApplicationState state,
        BrowserTransition transition,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration,
        ProjectTodoMutationService? mutationService)
    {
        if (mutationService is null || transition.BulkUpdate is null || transition.TodoIdentities.IsDefaultOrEmpty)
        {
            return (state with
            {
                Browser = state.Browser with
                {
                    Error = "Bulk todo writing is unavailable.",
                    StatusMessage = null
                }
            }, catalog);
        }

        var expectedCatalog = catalog;
        catalog = catalogLoader.Load(configuration.ProjectFiles);

        var succeeded = new HashSet<TodoIdentity>();
        var failures = new List<string>();
        foreach (var group in transition.TodoIdentities.GroupBy(identity => identity.ProjectPath))
        {
            var groupIdentities = group.ToArray();
            var expected = groupIdentities
                .Select(identity => FindTodo(expectedCatalog, identity))
                .ToArray();
            TodoMutationResult result;
            if (expected.Any(todo => todo is null))
            {
                result = TodoMutationResult.Failure("A selected todo cannot be found.");
            }
            else
            {
                result = mutationService.UpdateMany(group.Key, expected.Select(todo => todo!).ToArray(), transition.BulkUpdate);
            }

            if (result.Succeeded)
            {
                succeeded.UnionWith(groupIdentities);
            }
            else
            {
                failures.Add($"{Path.GetFileNameWithoutExtension(group.Key)}: {result.Error}");
            }
        }

        if (succeeded.Count > 0)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
        }

        var remaining = state.Browser.MarkedTodos.Except(succeeded).ToImmutableHashSet();
        var successText = $"Updated {succeeded.Count} task(s) in " +
                          $"{transition.TodoIdentities.Where(succeeded.Contains).Select(id => id.ProjectPath).Distinct().Count()} project(s).";
        var error = failures.Count == 0
            ? null
            : $"{successText} {remaining.Count} task(s) failed. {string.Join(" ", failures)}";
        return (state with
        {
            Browser = state.Browser with
            {
                MarkedTodos = remaining,
                BulkEditor = failures.Count == 0
                    ? null
                    : state.Browser.BulkEditor is null
                        ? null
                        : state.Browser.BulkEditor with
                        {
                            SelectedCount = remaining.Count,
                            Error = error
                        },
                Error = error,
                StatusMessage = error is null ? successText : null,
                PendingTodoSelection = null
            }
        }, catalog);
    }

    private (ApplicationState State, ProjectCatalog Catalog) ApplyExternalEdit(
        ApplicationState state,
        BrowserTransition transition,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration)
    {
        if (externalEditorLauncher is null ||
            transition.ProjectPath is null ||
            transition.TodoIdentity is null)
        {
            return (state with
            {
                Browser = state.Browser with { Error = "External editing is unavailable." }
            }, catalog);
        }

        ExternalEditorResult result;
        terminalUi.SuspendForExternalProcess();
        try
        {
            result = externalEditorLauncher.Open(
                transition.ProjectPath,
                transition.TodoIdentity.SourceLine);
        }
        finally
        {
            terminalUi.ResumeAfterExternalProcess();
        }

        if (result.Started)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
        }

        return (state with
        {
            Browser = state.Browser with
            {
                PendingTodoSelection = null,
                Error = result.Error,
                MarkedTodos = result.Started ? [] : state.Browser.MarkedTodos,
                BulkEditor = result.Started ? null : state.Browser.BulkEditor,
                StatusMessage = null
            }
        }, catalog);
    }

    private static TodoMutationResult ApplyOperation(
        BrowserTransition transition,
        ProjectCatalog expectedCatalog,
        ProjectTodoMutationService? mutationService,
        DateOnly today)
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
            var expectedProject = expectedCatalog.Projects.FirstOrDefault(
                project => project.Path == transition.ProjectPath);
            return expectedProject is null
                ? TodoMutationResult.Failure("The selected project cannot be found.")
                : mutationService.RollOverdueToDate(transition.ProjectPath, expectedProject, today);
        }

        var expected = FindTodo(expectedCatalog, transition.TodoIdentity);
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

    private static TodoItem? FindTodo(ProjectCatalog catalog, TodoIdentity? identity)
    {
        if (identity is null)
        {
            return null;
        }

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
            foreach (var subtask in Flatten(todo.Subtasks))
            {
                yield return subtask;
            }
        }
    }
}
