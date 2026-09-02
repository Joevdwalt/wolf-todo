using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Cli.Tests.Features;

public sealed class MultiFileSystem(IReadOnlyDictionary<string, string> files) : IProjectFileSystem
{
    public bool FileExists(string candidate) => files.ContainsKey(candidate);
    public string GetFullPath(string candidate) => candidate;
    public string ReadAllText(string candidate) => files[candidate];
    public void WriteAllTextAtomically(string candidate, string value) =>
        throw new InvalidOperationException("No write expected.");
}
