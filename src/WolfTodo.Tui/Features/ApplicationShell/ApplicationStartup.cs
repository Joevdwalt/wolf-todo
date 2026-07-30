using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class ApplicationStartup(
    IApplicationConfigurationLoader configurationLoader,
    ProjectCatalogLoader catalogLoader,
    IApplicationStateStore stateStore,
    ITerminalUi terminalUi,
    Func<DateOnly> todayProvider)
{
    public ApplicationStartupResult Start()
    {
        try
        {
            return ApplicationStartupResult.Success(CreateRuntime(configurationLoader.Load()));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            terminalUi.ShowStartupError(exception.Message);
            return ApplicationStartupResult.Failure(1);
        }
    }

    private ApplicationRuntime CreateRuntime(ApplicationConfiguration configuration)
    {
        var catalog = catalogLoader.Load(configuration.ProjectFiles);
        var session = stateStore.Load();
        var browser = BrowserState.Initial with
        {
            ProjectIndex = FindProjectIndex(
                catalog,
                session.SelectedProjectPath,
                configuration.SidebarItems.Length),
            Focus = BrowserFocus.Todos,
            Sort = session.Sort
        };
        var state = new ApplicationState(TabHostState.CreateInitial(ApplicationTabs.All), browser)
        {
            Planner = PlannerState.CreateInitial(todayProvider())
        };
        return new ApplicationRuntime(configuration, catalog, state, session.SelectedProjectPath);
    }

    public static int FindProjectIndex(
        ProjectCatalog catalog,
        string? selectedProjectPath,
        int savedSidebarItemCount)
    {
        if (selectedProjectPath is null)
        {
            return 0;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var projectIndex = FindPath(catalog.Projects.Select(project => project.Path), selectedProjectPath, comparison);
        if (projectIndex >= 0)
        {
            return projectIndex + savedSidebarItemCount + 2;
        }

        var errorIndex = FindPath(catalog.Errors.Select(error => error.Path), selectedProjectPath, comparison);
        return errorIndex < 0
            ? 0
            : catalog.Projects.Length + savedSidebarItemCount + errorIndex + 2;
    }

    private static int FindPath(
        IEnumerable<string> paths,
        string selectedPath,
        StringComparison comparison) =>
        paths.Select((path, index) => (path, index))
            .FirstOrDefault(candidate => string.Equals(candidate.path, selectedPath, comparison), (null!, -1))
            .index;
}
