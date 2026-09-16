using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationFrameResult(
    ApplicationState State,
    ProjectCatalog Catalog,
    BrowserView? BrowserView,
    PlannerView? PlannerView,
    FocusedTaskView? FocusedTaskView,
    CommandPaletteView? PaletteView,
    SidebarSelectionAnchor SelectionAnchor,
    string? SelectedProjectPath,
    bool Retry)
{
    public static ApplicationFrameResult RetryFrame(
        ApplicationState state,
        ProjectCatalog catalog,
        SidebarSelectionAnchor selectionAnchor,
        string? selectedProjectPath) => new(
        state,
        catalog,
        null,
        null,
        null,
        null,
        selectionAnchor,
        selectedProjectPath,
        true);
}
