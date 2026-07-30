using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationRuntime(
    ApplicationConfiguration Configuration,
    ProjectCatalog Catalog,
    ApplicationState State,
    string? SelectedProjectPath);
