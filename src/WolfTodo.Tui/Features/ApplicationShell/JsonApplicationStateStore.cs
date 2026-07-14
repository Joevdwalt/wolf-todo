using System.Text.Json;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class JsonApplicationStateStore(string path) : IApplicationStateStore
{
    public string? LoadSelectedProjectPath()
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var state = JsonSerializer.Deserialize<PersistedApplicationState>(File.ReadAllText(path));
            return state?.SelectedProjectPath;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public void SaveSelectedProjectPath(string? selectedProjectPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var state = new PersistedApplicationState(selectedProjectPath);
            File.WriteAllText(path, JsonSerializer.Serialize(state));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Session state is best-effort and must not prevent the application from exiting.
        }
    }

    private sealed record PersistedApplicationState(string? SelectedProjectPath);
}
