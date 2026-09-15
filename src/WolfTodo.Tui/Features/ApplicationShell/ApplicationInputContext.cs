using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationInputContext(
    ApplicationState State,
    ProjectCatalog Catalog,
    ApplicationConfiguration Configuration,
    ImmutableArray<TabDefinition> Tabs,
    BrowserView? BrowserView,
    PlannerView? PlannerView,
    FocusedTaskView? FocusedTaskView,
    CommandPaletteView? PaletteView);
