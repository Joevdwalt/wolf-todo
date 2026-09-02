using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Cli.Tests.Features;

public sealed class MemoryFileSystem(string path, string contents) : IProjectFileSystem
{
    public int WriteCount { get; private set; }
    public bool FileExists(string candidate) => candidate == path;
    public string GetFullPath(string candidate) => candidate;
    public string ReadAllText(string candidate) => candidate == path ? contents : throw new FileNotFoundException();
    public void WriteAllTextAtomically(string candidate, string value) => WriteCount++;
}
