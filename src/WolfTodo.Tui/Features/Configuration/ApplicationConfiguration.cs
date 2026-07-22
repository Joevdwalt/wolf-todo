using System.Collections.Immutable;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.Configuration;

public sealed record ApplicationConfiguration(
    ImmutableArray<string> ProjectFiles,
    TuiKeyBindings KeyBindings)
{
    public TuiTheme Theme { get; init; } = TuiThemes.Wolf;

    public GoogleCalendarConfiguration GoogleCalendar { get; init; } = GoogleCalendarConfiguration.Disabled;

    public PlannerConfiguration Planner { get; init; } = PlannerConfiguration.Default;

    public ImmutableArray<SavedSidebarView> SidebarItems { get; init; } = [];

    public ApplicationConfiguration(ImmutableArray<string> projectFiles, string quitCommand)
        : this(projectFiles, TuiKeyBindings.CreateDefaults(quitCommand))
    {
    }

    public string QuitCommand => KeyBindings.QuitCommand;
}
