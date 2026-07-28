using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Infrastructure;

public sealed class PhysicalDayScheduleMarkdownFileStore : IDayScheduleMarkdownFileStore
{
    public bool FileExists(string path) => File.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllTextAtomically(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new IOException($"Cannot determine the directory for {path}.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, contents);
            if (File.Exists(path) && !OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(temporaryPath, File.GetUnixFileMode(path));
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
