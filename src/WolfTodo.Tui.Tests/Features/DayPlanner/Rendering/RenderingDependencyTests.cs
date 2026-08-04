using FluentAssertions;

namespace WolfTodo.Tui.Tests.Features.DayPlanner.Rendering;

public sealed class RenderingDependencyTests
{
    [Fact]
    public void Day_planner_and_application_shell_renderers_do_not_call_browser_renderer()
    {
        var sourceRoot = Path.Combine(RepositoryRoot(), "src", "WolfTodo.Tui");
        var guardedDirectories = new[]
        {
            Path.Combine(sourceRoot, "Features", "DayPlanner", "Rendering"),
            Path.Combine(sourceRoot, "Features", "ApplicationShell", "Rendering")
        };

        var offenders = guardedDirectories
            .SelectMany(directory => Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            .Where(file => File.ReadAllText(file).Contains("BrowserRenderer.", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(sourceRoot, file))
            .ToArray();

        offenders.Should().BeEmpty("planner, calendar, and status rendering should own their behavior directly");
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
