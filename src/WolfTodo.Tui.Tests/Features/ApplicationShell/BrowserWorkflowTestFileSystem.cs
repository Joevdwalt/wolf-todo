using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

internal sealed class BrowserWorkflowTestFileSystem : IProjectFileSystem
{
    public string Contents { get; private set; } = "# Work\n\n- [ ] First\n- [ ] Second\n- [ ] Third\n";
    public bool FailWrites { get; init; }
    public bool FileExists(string path) => path == "/work.md";
    public string GetFullPath(string path) => path;
    public string ReadAllText(string path) => Contents;

    public void WriteAllTextAtomically(string path, string contents)
    {
        if (FailWrites) throw new IOException("write failed");
        Contents = contents;
    }
}
