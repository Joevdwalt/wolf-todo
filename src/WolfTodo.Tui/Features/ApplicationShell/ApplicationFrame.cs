using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationFrame(
    ApplicationRuntime Runtime,
    TabStripView Tabs,
    BrowserView? Browser,
    PlannerView? Planner,
    CommandPaletteView? Palette,
    bool FeatureCapturesInput,
    TimeSpan? ReadTimeout);
