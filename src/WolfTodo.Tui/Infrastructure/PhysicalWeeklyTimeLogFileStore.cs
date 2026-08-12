using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Infrastructure;

public sealed class PhysicalWeeklyTimeLogFileStore : IWeeklyTimeLogFileStore
{
    public bool FileExists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllTextAtomically(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new IOException($"Cannot determine the directory for {path}.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, contents);
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
