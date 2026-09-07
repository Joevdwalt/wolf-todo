using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record SidebarSelectionAnchor(ProjectRowKind Kind, string? Identifier)
{
    public static SidebarSelectionAnchor Capture(BrowserView view)
    {
        var selected = view.Projects.First(project => project.IsSelected);
        return new SidebarSelectionAnchor(
            selected.Kind,
            selected.Kind switch
            {
                ProjectRowKind.SavedQuery => selected.Title,
                ProjectRowKind.Project => selected.Project?.Path,
                ProjectRowKind.Error => selected.Error?.Path,
                _ => null
            });
    }
}
