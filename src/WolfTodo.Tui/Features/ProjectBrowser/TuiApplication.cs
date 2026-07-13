using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed class TuiApplication(
    IApplicationConfigurationLoader configurationLoader,
    ProjectCatalogLoader catalogLoader,
    ITerminalUi terminalUi,
    ProjectBrowserPresenter presenter,
    BrowserReducer reducer,
    string logo)
{
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
        terminalUi.ShowSplash(logo);
        terminalUi.ReadKey();

        var state = BrowserState.Initial;

        while (true)
        {
            var view = presenter.CreateView(catalog, state);
            state = view.State;
            terminalUi.ShowBrowser(view, configuration.KeyBindings);

            var transition = reducer.Reduce(state, terminalUi.ReadKey(), configuration, view);

            if (transition.ShouldExit)
            {
                return 0;
            }

            state = transition.State;
        }
    }
}
