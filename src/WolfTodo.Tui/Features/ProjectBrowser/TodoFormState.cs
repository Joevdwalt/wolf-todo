using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoFormState(
    bool IsCreate,
    string? ProjectPath,
    int ProjectPickerIndex,
    TodoFormField Field,
    bool IsEditing,
    string Draft,
    TodoUpdate Values,
    TodoIdentity? Target,
    string? Error)
{
    public bool IsChoosingProject => ProjectPath is null;

    public string ScheduledDate { get; init; } = Values.Schedule?.Date.ToString("yyyy-MM-dd") ?? string.Empty;

    public string ScheduledTime { get; init; } = Values.Schedule?.Time.ToString("HH:mm") ?? string.Empty;

    public bool ScheduleRequired { get; init; }
}
