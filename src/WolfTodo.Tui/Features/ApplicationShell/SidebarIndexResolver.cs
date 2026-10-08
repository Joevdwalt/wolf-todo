using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public static class SidebarIndexResolver
{
    public static int Resolve(
        SidebarSelectionAnchor selection,
        ProjectCatalog catalog,
        IReadOnlyList<SavedSidebarView> sidebarItems)
    {
        if (selection.Kind == ProjectRowKind.All) return 0;
        if (selection.Kind == ProjectRowKind.SavedQuery)
        {
            var savedIndex = sidebarItems
                .Select((item, index) => (item, index))
                .FirstOrDefault(candidate => candidate.item.Title == selection.Identifier).index;
            return sidebarItems.Any(item => item.Title == selection.Identifier) ? savedIndex + 1 : 0;
        }

        return ResolveProjectPath(selection.Identifier, catalog, sidebarItems.Count);
    }

    public static int ResolveProjectPath(string? path, ProjectCatalog catalog, int savedViewCount)
    {
        if (path is null) return 0;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        for (var index = 0; index < catalog.Projects.Length; index++)
        {
            if (string.Equals(catalog.Projects[index].Path, path, comparison))
                return index + savedViewCount + 1;
        }

        for (var index = 0; index < catalog.Errors.Length; index++)
        {
            if (string.Equals(catalog.Errors[index].Path, path, comparison))
                return catalog.Projects.Length + savedViewCount + index + 1;
        }

        return 0;
    }
}
