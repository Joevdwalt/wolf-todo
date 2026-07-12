namespace WolfTodo.Core.Features.ProjectBrowser;

public interface IProjectFileSystem
{
    bool FileExists(string path);

    string GetFullPath(string path);

    string ReadAllText(string path);
}
