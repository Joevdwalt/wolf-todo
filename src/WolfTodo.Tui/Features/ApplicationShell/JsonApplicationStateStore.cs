using System.Text.Json;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class JsonApplicationStateStore(string path) : IApplicationStateStore
{
    public ApplicationSessionState Load()
    {
        try
        {
            if (!File.Exists(path))
            {
                return ApplicationSessionState.Initial;
            }

            var state = JsonSerializer.Deserialize<PersistedApplicationState>(File.ReadAllText(path));
            if (state is null)
            {
                return ApplicationSessionState.Initial;
            }

            var sort = state.Sort is not null &&
                       Enum.IsDefined(state.Sort.Property) &&
                       Enum.IsDefined(state.Sort.Direction)
                ? state.Sort
                : TodoSort.Source;
            return new ApplicationSessionState(state.SelectedProjectPath, sort);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return ApplicationSessionState.Initial;
        }
    }

    public void Save(ApplicationSessionState state)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var persisted = new PersistedApplicationState(state.SelectedProjectPath, state.Sort);
            File.WriteAllText(path, JsonSerializer.Serialize(persisted));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Session state is best-effort and must not prevent the application from exiting.
        }
    }

    private sealed record PersistedApplicationState(
        string? SelectedProjectPath,
        TodoSort? Sort = null);
}
