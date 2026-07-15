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
}
