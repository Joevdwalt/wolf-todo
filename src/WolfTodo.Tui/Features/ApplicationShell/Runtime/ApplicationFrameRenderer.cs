using WolfTodo.Tui.Features.ApplicationShell.Actions;
using WolfTodo.Tui.Features.ApplicationShell.CommandPalette;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell.Runtime;

public sealed class ApplicationFrameRenderer(
    ITerminalUi terminalUi,
    TabHostPresenter tabPresenter,
    ProjectBrowserPresenter browserPresenter,
    DayPlannerPresenter plannerPresenter,
    CommandPalettePresenter palettePresenter,
    ApplicationActionCatalog actionCatalog,
    PlannerCalendarAgendaCache calendarCache)
{
    public ApplicationFrame Render(ApplicationRuntime runtime)
    {
        EnsureSupported(runtime.State.Tabs.ActiveTab);
        return runtime.State.Tabs.ActiveTab == ApplicationTabs.Todos
            ? RenderBrowser(runtime)
            : RenderPlanner(runtime);
    }

    private ApplicationFrame RenderBrowser(ApplicationRuntime runtime)
    {
        var configuration = runtime.Configuration;
        var view = browserPresenter.CreateView(
            runtime.Catalog,
            runtime.State.Browser,
            configuration.SidebarItems);
        var state = runtime.State with { Browser = view.State };
        var updated = runtime with
        {
            State = state,
            SelectedProjectPath = view.SelectedProjectPath
        };
        var palette = CreatePalette(updated, view, null);
        terminalUi.ShowBrowser(
            CreateTabs(state),
            view with
            {
                GlobalCommand = state.Command.IsActive ? state.Command.Value : null,
                GlobalError = state.Command.Error,
                CommandPalette = palette
            },
            configuration.KeyBindings,
            configuration.Theme);
        return new ApplicationFrame(
            updated,
            CreateTabs(state),
            view,
            null,
            palette,
            state.Browser.IsFilterMode || state.Browser.IsSortMode || state.Browser.Editor is not null,
            null);
    }

    private ApplicationFrame RenderPlanner(ApplicationRuntime runtime)
    {
        var configuration = runtime.Configuration;
        var agenda = calendarCache.GetAgenda(
            configuration.GoogleCalendar,
            runtime.State.Planner.SelectedDate);
        var view = plannerPresenter.CreateView(
            runtime.Catalog,
            runtime.State.Planner,
            agenda,
            configuration.Planner);
        var state = runtime.State with { Planner = view.State };
        var updated = runtime with { State = state };
        var palette = CreatePalette(updated, null, view);
        terminalUi.ShowPlanner(
            CreateTabs(state),
            view with
            {
                GlobalCommand = state.Command.IsActive ? state.Command.Value : null,
                GlobalError = state.Command.Error,
                CommandPalette = palette
            },
            configuration.KeyBindings,
            configuration.Theme);
        return new ApplicationFrame(
            updated,
            CreateTabs(state),
            null,
            view,
            palette,
            state.Planner.CapturesInput,
            calendarCache.IsRefreshing
                ? TimeSpan.FromMilliseconds(250)
                : TimeSpan.FromMinutes(1));
    }

    private CommandPaletteView? CreatePalette(
        ApplicationRuntime runtime,
        BrowserView? browser,
        PlannerView? planner) =>
        runtime.State.Palette.IsOpen
            ? palettePresenter.CreateView(
                runtime.State.Palette,
                actionCatalog.Create(
                    runtime.State.Tabs.ActiveTab == ApplicationTabs.Todos,
                    browser,
                    planner,
                    runtime.Configuration.KeyBindings,
                    runtime.Configuration.Planner.Export is not null))
            : null;

    private TabStripView CreateTabs(ApplicationState state) =>
        tabPresenter.CreateView(ApplicationTabs.All, state.Tabs);

    private static void EnsureSupported(TabId activeTab)
    {
        if (activeTab != ApplicationTabs.Todos && activeTab != ApplicationTabs.Planner)
        {
            throw new InvalidOperationException($"No feature is registered for tab '{activeTab.Value}'.");
        }
    }
}
