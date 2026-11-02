using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record BrowserSingleUpdateRequest(
    ApplicationState State,
    BrowserTransition Transition,
    ProjectCatalog Catalog,
    ApplicationConfiguration Configuration,
    ProjectTodoMutationService? MutationService);
