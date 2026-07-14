using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class TuiApplication(
    IApplicationConfigurationLoader configurationLoader,
    ProjectCatalogLoader catalogLoader,
    ITerminalUi terminalUi,
    ApplicationInputRouter inputRouter,
    TabHostPresenter tabPresenter,
    TabHostReducer tabReducer,
    ProjectBrowserPresenter browserPresenter,
    BrowserReducer browserReducer,
    string logo)
{
    private static readonly TabId TodosTab = new("todos");
    private static readonly ImmutableArray<TabDefinition> Tabs =
    [
        new(TodosTab, "Todos")
    ];

    public int Run()
    {
        ApplicationConfiguration configuration;

        try
        {
            configuration = configurationLoader.Load();
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            terminalUi.ShowStartupError(exception.Message);
            return 1;
        }

        var catalog = catalogLoader.Load(configuration.ProjectFiles);
        terminalUi.SetCursorVisible(false);

        try
        {
            terminalUi.ShowSplash(logo);
            terminalUi.ReadKey();

            var state = ApplicationState.CreateInitial(TabHostState.CreateInitial(Tabs));

            while (true)
            {
                EnsureSupportedTab(state.Tabs.ActiveTab);
                var tabView = tabPresenter.CreateView(Tabs, state.Tabs);
                var browserView = browserPresenter.CreateView(catalog, state.Browser);
                state = state with { Browser = browserView.State };
                terminalUi.ShowBrowser(tabView, browserView, configuration.KeyBindings);

                var key = terminalUi.ReadKey();

                var inputRoute = inputRouter.Route(
                    state.Browser.IsCommandMode || state.Browser.IsFilterMode,
                    key,
                    configuration.KeyBindings);

                if (inputRoute is ApplicationInputRoute.NextTab or ApplicationInputRoute.PreviousTab)
                {
                    var direction = inputRoute == ApplicationInputRoute.PreviousTab
                        ? TabDirection.Previous
                        : TabDirection.Next;
                    state = state with { Tabs = tabReducer.Move(state.Tabs, Tabs, direction) };
                    continue;
                }

                var transition = browserReducer.Reduce(state.Browser, key, configuration, browserView);

                if (transition.ShouldExit)
                {
                    return 0;
                }

                state = state with { Browser = transition.State };
            }
        }
        finally
        {
            terminalUi.SetCursorVisible(true);
        }
    }

    private static void EnsureSupportedTab(TabId activeTab)
    {
        if (activeTab != TodosTab)
        {
            throw new InvalidOperationException($"No feature is registered for tab '{activeTab.Value}'.");
        }
    }
}
