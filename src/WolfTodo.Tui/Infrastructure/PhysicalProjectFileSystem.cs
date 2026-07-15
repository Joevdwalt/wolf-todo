using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Infrastructure;

public sealed class PhysicalProjectFileSystem : IProjectFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public string GetFullPath(string path) => Path.GetFullPath(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllTextAtomically(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new IOException($"Cannot determine the directory for {path}.");
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, contents);
            if (!OperatingSystem.IsWindows())
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
