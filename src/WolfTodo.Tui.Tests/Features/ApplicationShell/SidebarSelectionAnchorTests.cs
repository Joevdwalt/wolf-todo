using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class SidebarSelectionAnchorTests
{
    [Fact]
    public void Capture_uses_the_canonical_path_for_a_project()
    {
        var project = new TodoProject("Work", "/todos/work.md", []);
        var view = new ProjectBrowserPresenter().CreateView(
            new ProjectCatalog([project], []),
            BrowserState.Initial with { ProjectIndex = 2 });

        SidebarSelectionAnchor.Capture(view).Should()
            .Be(new SidebarSelectionAnchor(ProjectRowKind.Project, "/todos/work.md"));
    }
}
