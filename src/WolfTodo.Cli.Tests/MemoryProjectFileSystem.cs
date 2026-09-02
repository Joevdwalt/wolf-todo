using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Cli.Tests;

public sealed class MemoryProjectFileSystem(string path, string contents) : IProjectFileSystem
{
    public string Contents { get; private set; } = contents;
    public int WriteCount { get; private set; }

    public bool FileExists(string candidate) => candidate == path;
    public string GetFullPath(string candidate) => candidate;
    public string ReadAllText(string candidate) => candidate == path
        ? Contents
        : throw new FileNotFoundException(candidate);

    public void WriteAllTextAtomically(string candidate, string value)
    {
        candidate.Should().Be(path);
        Contents = value;
        WriteCount++;
    }
}
