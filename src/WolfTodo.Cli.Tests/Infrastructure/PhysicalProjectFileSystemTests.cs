using FluentAssertions;
using WolfTodo.Cli.Infrastructure;

namespace WolfTodo.Cli.Tests.Infrastructure;

public sealed class PhysicalProjectFileSystemTests
{
    [Fact]
    public void WriteAllTextAtomically_replaces_the_project_contents()
    {
        var directory = Directory.CreateTempSubdirectory("wtodo-cli-test-");
        var path = Path.Combine(directory.FullName, "project.md");
        try
        {
            File.WriteAllText(path, "before");
            var fileSystem = new PhysicalProjectFileSystem();

            fileSystem.WriteAllTextAtomically(path, "after");

            fileSystem.ReadAllText(path).Should().Be("after");
            Directory.GetFiles(directory.FullName).Should().Equal(path);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
