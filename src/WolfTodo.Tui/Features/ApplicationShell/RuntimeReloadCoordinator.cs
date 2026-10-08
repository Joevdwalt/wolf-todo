using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class RuntimeReloadCoordinator(
    IApplicationConfigurationLoader configurationLoader,
    ProjectCatalogLoader catalogLoader,
    IApplicationFileChangeMonitor fileChangeMonitor,
    PlannerCalendarAgendaCache plannerCalendarCache)
{
    public RuntimeReloadResult Reload(
        ApplicationState state,
        ApplicationConfiguration configuration,
        ProjectCatalog catalog,
        SidebarSelectionAnchor selection,
        ApplicationFileChanges changes)
    {
        var configurationReloaded = false;
        Exception? configurationError = null;
        if (changes.ConfigurationChanged)
        {
            try
            {
                configuration = configurationLoader.Load();
                configurationReloaded = true;
                fileChangeMonitor.WatchProjectFiles(configuration.ProjectFiles);
                plannerCalendarCache.RefreshWindow(configuration.GoogleCalendar);
            }
            catch (Exception exception) when (exception is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                configurationError = exception;
            }
        }

        if (configurationReloaded || changes.ProjectFilesChanged)
        {
            catalog = catalogLoader.Load(configuration.ProjectFiles);
            state = state with
            {
                Browser = state.Browser with
                {
                    ProjectIndex = SidebarIndexResolver.Resolve(selection, catalog, configuration.SidebarItems),
                    PendingTodoSelection = null,
                    StatusMessage = null
                }
            };
        }

        var status = configurationError is not null
            ? new RuntimeReloadStatus(
                $"Configuration reload failed: {configurationError.Message} Using the last valid configuration.",
                true,
                false)
            : state.ReloadStatus is { IsError: true } existingError && !configurationReloaded
                ? existingError
                : new RuntimeReloadStatus(
                configurationReloaded && changes.ProjectFilesChanged
                    ? "Configuration and projects reloaded."
                    : configurationReloaded
                        ? "Configuration reloaded."
                        : "Projects reloaded.",
                false,
                true);

        return new RuntimeReloadResult(state with { ReloadStatus = status }, configuration, catalog);
    }

}
