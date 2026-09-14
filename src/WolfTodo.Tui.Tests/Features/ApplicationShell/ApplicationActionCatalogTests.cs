using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class ApplicationActionCatalogTests
{
    [Fact]
    public void Create_exposes_opening_and_disables_generation_without_a_markdown_selection()
    {
        var bindings = TuiKeyBindings.CreateDefaults(":q");
        var browser = new ProjectBrowserPresenter().CreateView(new ProjectCatalog([], []), BrowserState.Initial);
        var items = new ApplicationActionCatalog().Create(true, browser, null, bindings);
        var generate = items.Single(item => item.Action == ApplicationActionId.GenerateTaskLink);
        generate.IsEnabled.Should().BeFalse();
        generate.DisabledReason.Should().NotBeNullOrEmpty();
        items.Single(item => item.Action == ApplicationActionId.OpenTaskLink).IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Create_keeps_both_link_actions_in_the_reduced_focus_palette()
    {
        var todo = new TodoItem(3, false, null, "Task", null, [], null, null, "", [], []);
        var project = new TodoProject("Work", "/todos/work.md", [todo]);
        var focus = FocusedTaskState.Create(new TodoIdentity(project.Path, 3), todo);
        var view = new FocusedTaskPresenter().CreateView(new ProjectCatalog([project], []), focus)!;
        var items = new ApplicationActionCatalog().Create(true, null, null,
            TuiKeyBindings.CreateDefaults(":q"), focusedTask: view);
        items.Single(item => item.Action == ApplicationActionId.GenerateTaskLink).IsEnabled.Should().BeTrue();
        items.Single(item => item.Action == ApplicationActionId.OpenTaskLink).IsEnabled.Should().BeTrue();
    }
}
