namespace WolfTodo.Tui.Features.Configuration;

public static class GlobalApplicationStatePath
{
    public static string Resolve()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "wtodo",
                "state.json");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(home, "Library", "Application Support", "wtodo", "state.json");
        }

        var xdgStateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
        var stateDirectory = string.IsNullOrWhiteSpace(xdgStateHome)
            ? Path.Combine(home, ".local", "state")
            : xdgStateHome;

        return Path.Combine(stateDirectory, "wtodo", "state.json");
    }
}
