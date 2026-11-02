using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class BrowserWorkflow(
    ProjectCatalogLoader catalogLoader,
    IExternalEditorLauncher? externalEditorLauncher,
    ITerminalUi terminalUi,
    Func<DateOnly> todayProvider,
    ProjectBrowserPresenter presenter)
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

        return ApplySingleUpdate(new BrowserSingleUpdateRequest(state, transition, catalog, configuration, mutationService));
    }

    public (ApplicationState State, ProjectCatalog Catalog) ApplySingleUpdate(BrowserSingleUpdateRequest request)
    {
        var (state, transition, catalog, configuration, mutationService) = request;
        state = state with { Browser = transition.State };
        var before = transition.Operation == BrowserOperation.ToggleCompleted
            ? presenter.CreateView(catalog, state.Browser, configuration.SidebarItems)
            : null;
        var expectedCatalog = catalog;
        catalog = catalogLoader.Load(configuration.ProjectFiles);
        var result = ApplyOperation(transition, expectedCatalog, mutationService, todayProvider());
        state = state with { Browser = ApplyMutationResult(state.Browser, transition, result) };
        if (result.Succeeded)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
            if (before is not null && transition.TodoIdentity is { } identity)
            {
                var after = presenter.CreateView(catalog, state.Browser, configuration.SidebarItems);
                var selected = TodoSelectionResolver.AfterCompletion(before, after, identity);
                state = state with { Browser = state.Browser with { PendingTodoSelection = selected, TodoIndex = 0 } };
            }
        }

        return (state, catalog);
    }

    public static BrowserState ApplyMutationResult(
        BrowserState state, BrowserTransition transition, TodoMutationResult result)
    {
        if (!result.Succeeded)
        {
            return state with
            {
                Error = result.Error,
                Editor = state.Editor is null ? null : state.Editor with { Error = result.Error },
                PendingTodoSelection = null,
                StatusMessage = null
            };
        }

        var selection = result.SourceLine is { } line && transition.ProjectPath is { } path
            ? new TodoIdentity(path, line)
            : transition.Operation == BrowserOperation.RollProjectToday ? transition.TodoIdentity : null;
        return state with
        {
            Error = result.Error,
            CollapsedTodos = [],
            Editor = null,
            PendingTodoSelection = selection,
            MarkedTodos = [],
            MarkedTodoSnapshots = ImmutableDictionary<TodoIdentity, TodoItem>.Empty,
            BulkEditor = null,
            StatusMessage = "Todo update saved."
        };
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
        var todo = source is null ? null : TodoTree.FindBySourceLine(source.Todos, identity.SourceLine);
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
        return (state with
        {
            Browser = state.Browser with
            {
                Focus = BrowserFocus.Todos,
                ProjectIndex = SidebarIndexResolver.ResolveProjectPath(
                    target.Path, catalog, configuration.SidebarItems.Length),
                TodoIndex = 0,
                PendingTodoSelection = result.SourceLine is { } sourceLine
                    ? new TodoIdentity(target.Path, sourceLine)
                    : null,
                Error = null,
                CollapsedTodos = [],
                MarkedTodos = [],
                MarkedTodoSnapshots = ImmutableDictionary<TodoIdentity, TodoItem>.Empty,
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
                CollapsedTodos = [],
                MarkedTodos = [],
                MarkedTodoSnapshots = ImmutableDictionary<TodoIdentity, TodoItem>.Empty,
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
            var result = ApplyBulkProject(group.Key, groupIdentities, transition, expectedCatalog, mutationService);

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
        var remainingSnapshots = state.Browser.MarkedTodoSnapshots
            .Where(pair => remaining.Contains(pair.Key))
            .ToImmutableDictionary();
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
                MarkedTodoSnapshots = remainingSnapshots,
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
                PendingTodoSelection = null,
                CollapsedTodos = succeeded.Count > 0 ? [] : state.Browser.CollapsedTodos
            }
        }, catalog);
    }

    private static TodoMutationResult ApplyBulkProject(
        string projectPath, TodoIdentity[] identities, BrowserTransition transition,
        ProjectCatalog expectedCatalog, ProjectTodoMutationService mutationService)
    {
        var expected = identities
            .Select(identity => transition.ExpectedTodos?.GetValueOrDefault(identity) ??
                                TodoTree.FindBySourceLine(expectedCatalog, identity.ProjectPath, identity.SourceLine))
            .ToArray();
        return expected.Any(todo => todo is null)
            ? TodoMutationResult.Failure("A selected todo cannot be found.")
            : mutationService.UpdateMany(projectPath, expected.Select(todo => todo!).ToArray(), transition.BulkUpdate!);
    }

    public (ApplicationState State, ProjectCatalog Catalog) ApplyExternalEdit(
        ApplicationState state,
        BrowserTransition transition,
        ProjectCatalog catalog,
        ApplicationConfiguration configuration)
    {
        state = state with { Browser = transition.State };
        if (externalEditorLauncher is null ||
            transition.ProjectPath is null ||
            transition.TodoIdentity is null)
        {
            return (state with
            {
                Browser = state.Browser with { Error = "External editing is unavailable." }
            }, catalog);
        }

        var result = new ExternalEditorSession(terminalUi, externalEditorLauncher)
            .Open(transition.ProjectPath, transition.TodoIdentity.SourceLine);

        if (result.Started)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
        }

        return (state with
        {
            Browser = state.Browser with
            {
                PendingTodoSelection = null,
                CollapsedTodos = result.Started ? [] : state.Browser.CollapsedTodos,
                Error = result.Error,
                MarkedTodos = result.Started ? [] : state.Browser.MarkedTodos,
                MarkedTodoSnapshots = result.Started
                    ? ImmutableDictionary<TodoIdentity, TodoItem>.Empty
                    : state.Browser.MarkedTodoSnapshots,
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

        var expected = transition.ExpectedTodo ?? (transition.TodoIdentity is { } todoIdentity
            ? TodoTree.FindBySourceLine(expectedCatalog, todoIdentity.ProjectPath, todoIdentity.SourceLine)
            : null);
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

}
