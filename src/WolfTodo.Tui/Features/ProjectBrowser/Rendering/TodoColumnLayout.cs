namespace WolfTodo.Tui.Features.ProjectBrowser.Rendering;

public sealed record TodoColumnLayout(
    int ContentWidth,
    int TaskWidth,
    bool ShowProject,
    int ProjectWidth,
    bool ShowSchedule,
    int ScheduleWidth);
