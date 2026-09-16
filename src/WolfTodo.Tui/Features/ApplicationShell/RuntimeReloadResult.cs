using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record RuntimeReloadResult(
    ApplicationState State,
    ApplicationConfiguration Configuration,
    ProjectCatalog Catalog);
