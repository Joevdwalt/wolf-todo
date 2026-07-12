using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Infrastructure;

public sealed class PhysicalProjectFileSystem : IProjectFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public string GetFullPath(string path) => Path.GetFullPath(path);

    public string ReadAllText(string path) => File.ReadAllText(path);
}
