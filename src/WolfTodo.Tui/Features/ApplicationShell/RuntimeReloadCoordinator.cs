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
                    ProjectIndex = ResolveProjectIndex(selection, catalog, configuration.SidebarItems),
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

    public static int ResolveProjectIndex(
        SidebarSelectionAnchor selection,
        ProjectCatalog catalog,
        IReadOnlyList<SavedSidebarView> sidebarItems)
    {
        if (selection.Kind == ProjectRowKind.All) return 0;
        if (selection.Kind == ProjectRowKind.Today) return 1;
        if (selection.Kind == ProjectRowKind.SavedQuery)
        {
            var savedIndex = sidebarItems
                .Select((item, index) => (item, index))
                .FirstOrDefault(candidate => candidate.item.Title == selection.Identifier).index;
            return sidebarItems.Any(item => item.Title == selection.Identifier) ? savedIndex + 2 : 0;
        }

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var projectIndex = catalog.Projects
            .Select((project, index) => (project, index))
            .FirstOrDefault(candidate => string.Equals(candidate.project.Path, selection.Identifier, comparison));
        if (projectIndex.project is not null)
        {
            return projectIndex.index + sidebarItems.Count + 2;
        }

        var errorIndex = catalog.Errors
            .Select((error, index) => (error, index))
            .FirstOrDefault(candidate => string.Equals(candidate.error.Path, selection.Identifier, comparison));
        return errorIndex.error is null
            ? 0
            : catalog.Projects.Length + sidebarItems.Count + errorIndex.index + 2;
    }
}
