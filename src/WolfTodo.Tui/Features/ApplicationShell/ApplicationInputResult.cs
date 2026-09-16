using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationInputResult(
    ApplicationState State,
    ProjectCatalog Catalog,
    bool Exit = false);
