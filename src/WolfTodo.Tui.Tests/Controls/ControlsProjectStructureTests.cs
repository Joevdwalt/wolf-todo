using System.Text.RegularExpressions;
using FluentAssertions;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class ControlsProjectStructureTests
{
    [Fact]
    public void Controls_types_use_matching_files_and_the_controls_namespace()
    {
        var controlsDirectory = Path.Combine(RepositoryRoot(), "src", "WolfTodo.Tui", "Controls");
        var files = Directory.GetFiles(controlsDirectory, "*.cs", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            Regex.Match(source, "^namespace ([^;]+);", RegexOptions.Multiline).Groups[1].Value
                .Should().Be("WolfTodo.Tui.Controls", file);

            var declarations = Regex.Matches(
                source,
                "^(?:public|internal|private) (?:sealed |static |abstract |partial )*(?:class|record|enum|interface) ([A-Za-z_][A-Za-z0-9_]*)",
                RegexOptions.Multiline);
            declarations.Should().ContainSingle(file);
            declarations[0].Groups[1].Value.Should().Be(Path.GetFileNameWithoutExtension(file), file);
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WolfTodo.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("tests need the repository root to validate source layout");
        return directory!.FullName;
    }
}
