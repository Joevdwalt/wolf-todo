namespace WolfTodo.Core.Features.ProjectBrowser;

public interface IProjectFileSystem
{
    bool FileExists(string path);

    string GetFullPath(string path);

    string ReadAllText(string path);

    void WriteAllTextAtomically(string path, string contents) =>
        throw new NotSupportedException("This project file system is read-only.");
}
