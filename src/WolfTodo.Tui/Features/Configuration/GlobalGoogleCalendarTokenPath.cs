namespace WolfTodo.Tui.Features.Configuration;

public static class GlobalGoogleCalendarTokenPath
{
    public static string Resolve()
    {
        var statePath = GlobalApplicationStatePath.Resolve();
        var directory = Path.GetDirectoryName(statePath) ?? AppContext.BaseDirectory;
        return Path.Combine(directory, "google-calendar-token");
    }
}
