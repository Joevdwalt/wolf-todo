namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed record ApplicationFileChanges(bool ConfigurationChanged, bool ProjectFilesChanged)
{
    public static ApplicationFileChanges None { get; } = new(false, false);

    public bool HasChanges => ConfigurationChanged || ProjectFilesChanged;

    public ApplicationFileChanges Merge(ApplicationFileChanges other) => new(
        ConfigurationChanged || other.ConfigurationChanged,
        ProjectFilesChanged || other.ProjectFilesChanged);
}
