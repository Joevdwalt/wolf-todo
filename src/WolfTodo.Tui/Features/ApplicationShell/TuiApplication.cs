using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class TuiApplication
{
    private readonly ApplicationRunner runner;

    public TuiApplication(ApplicationRunner runner)
    {
        this.runner = runner;
    }

    public TuiApplication(
        IApplicationConfigurationLoader configurationLoader,
        ProjectCatalogLoader catalogLoader,
        ITerminalUi terminalUi,
        IApplicationStateStore applicationStateStore,
        ApplicationInputRouter inputRouter,
        TabHostPresenter tabPresenter,
        TabHostReducer tabReducer,
        ProjectBrowserPresenter browserPresenter,
        BrowserReducer browserReducer,
        string logo,
        DayPlannerPresenter? plannerPresenter = null,
        DayPlannerReducer? plannerReducer = null,
        ProjectTodoMutationService? mutationService = null,
        ApplicationCommandReducer? commandReducer = null,
        CommandPaletteReducer? paletteReducer = null,
        CommandPalettePresenter? palettePresenter = null,
        ApplicationActionCatalog? actionCatalog = null,
        IExternalEditorLauncher? externalEditorLauncher = null,
        PlannerCalendarAgendaCache? plannerCalendarCache = null,
        Func<DateOnly>? todayProvider = null,
        DayScheduleExportService? dayScheduleExportService = null)
        : this(new ApplicationRunner(
            configurationLoader, catalogLoader, terminalUi, applicationStateStore, inputRouter,
            tabPresenter, tabReducer, browserPresenter, browserReducer, logo, plannerPresenter,
            plannerReducer, mutationService, commandReducer, paletteReducer, palettePresenter,
            actionCatalog, externalEditorLauncher, plannerCalendarCache, todayProvider,
            dayScheduleExportService))
    {
    }

    public int Run() => runner.Run();
}
