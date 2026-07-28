namespace WolfTodo.Core.Features.ProjectBrowser;

/// <summary>Reads one Markdown-backed todo project from durable storage.</summary>
public interface ITodoProjectRepository
{
    string CanonicalizePath(string path);

    TodoProjectReadResult Read(string path);
}
