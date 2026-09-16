using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Infrastructure.Files;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class PhysicalApplicationFileChangeMonitorTests
{
    [Fact]
    public async Task Poll_reports_only_watched_files_after_debouncing()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wolf-todo-monitor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var configuration = Path.Combine(directory, "config.toml");
        var project = Path.Combine(directory, "project.md");
        var unrelated = Path.Combine(directory, "other.md");
        await File.WriteAllTextAsync(configuration, "[projects]", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(project, "# Before", TestContext.Current.CancellationToken);

        try
        {
            using var monitor = new PhysicalApplicationFileChangeMonitor(configuration);
            monitor.WatchProjectFiles([project]);
            await File.WriteAllTextAsync(unrelated, "ignored", TestContext.Current.CancellationToken);
            await Task.Delay(250, TestContext.Current.CancellationToken);
            monitor.Poll().Should().Be(ApplicationFileChanges.None);

            await File.WriteAllTextAsync(project, "# After", TestContext.Current.CancellationToken);
            var changes = await EventuallyChanged(monitor);

            changes.Should().Be(new ApplicationFileChanges(false, true));

            await File.WriteAllTextAsync(configuration, "[projects]\nfiles = []", TestContext.Current.CancellationToken);
            changes = await EventuallyChanged(monitor);

            changes.Should().Be(new ApplicationFileChanges(true, false));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<ApplicationFileChanges> EventuallyChanged(IApplicationFileChangeMonitor monitor)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await Task.Delay(25, TestContext.Current.CancellationToken);
            var changes = monitor.Poll();
            if (changes.HasChanges) return changes;
        }

        return ApplicationFileChanges.None;
    }
}
