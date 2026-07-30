using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell.Actions;
using WolfTodo.Tui.Features.ApplicationShell.CommandPalette;
using WolfTodo.Tui.Features.ApplicationShell.Commands;
using WolfTodo.Tui.Features.ApplicationShell.ExternalEditing;
using WolfTodo.Tui.Features.ApplicationShell.Input;
using WolfTodo.Tui.Features.ApplicationShell.Persistence;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell.Runtime;

public static class ApplicationRuntimeComposition
{
    public static ApplicationRunner Create(
        IApplicationConfigurationLoader configurationLoader,
        ProjectCatalogLoader catalogLoader,
        ITerminalUi terminalUi,
        IApplicationStateStore stateStore,
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
        PlannerCalendarAgendaCache? calendarCache = null,
        Func<DateOnly>? todayProvider = null,
        DayScheduleExportService? exportService = null)
    {
        var today = todayProvider ?? (() => DateOnly.FromDateTime(DateTime.Today));
        var plannerPresenterValue = plannerPresenter ?? new DayPlannerPresenter();
        var plannerReducerValue = plannerReducer ?? new DayPlannerReducer();
        var commandReducerValue = commandReducer ?? new ApplicationCommandReducer();
        var paletteReducerValue = paletteReducer ?? new CommandPaletteReducer();
        var palettePresenterValue = palettePresenter ?? new CommandPalettePresenter();
        var actionCatalogValue = actionCatalog ?? new ApplicationActionCatalog(today);
        var calendarCacheValue = calendarCache ??
            new PlannerCalendarAgendaCache(new DisabledPlannerCalendarAgendaProvider());
        var externalEditor = new ExternalTodoEditorExecutor(terminalUi, externalEditorLauncher);
        var browserTransitions = new BrowserTransitionExecutor(
            catalogLoader,
            today,
            mutationService,
            externalEditor);
        var plannerTransitions = new PlannerTransitionExecutor(
            catalogLoader,
            mutationService,
            externalEditor,
            exportService);
        var browserInput = new BrowserInputHandler(browserReducer, browserTransitions);
        var plannerInput = new PlannerInputHandler(
            plannerReducerValue,
            calendarCacheValue,
            plannerTransitions);
        var tabInput = new ApplicationTabInputHandler(tabReducer);
        var actionDispatcher = new ApplicationActionDispatcher(
            tabInput,
            browserInput,
            plannerInput);
        var commandInput = new ApplicationCommandInputHandler(
            commandReducerValue,
            actionDispatcher,
            browserInput);
        var paletteInput = new CommandPaletteInputHandler(
            paletteReducerValue,
            palettePresenterValue,
            actionCatalogValue,
            actionDispatcher);
        var startup = new ApplicationStartup(
            configurationLoader,
            catalogLoader,
            stateStore,
            terminalUi,
            today);
        var frames = new ApplicationFrameRenderer(
            terminalUi,
            tabPresenter,
            browserPresenter,
            plannerPresenterValue,
            palettePresenterValue,
            actionCatalogValue,
            calendarCacheValue);
        var input = new ApplicationInputDispatcher(
            inputRouter,
            tabInput,
            commandInput,
            paletteInput,
            browserInput,
            plannerInput);
        return new ApplicationRunner(
            startup,
            new ApplicationSession(terminalUi, stateStore, frames, input, logo));
    }
}
