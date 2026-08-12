using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class WeeklyTimeLogServiceTests
{
    [Fact]
    public void Record_writes_a_dated_session_to_the_weekly_markdown_file()
    {
        var store = new MemoryStore();
        var service = new WeeklyTimeLogService(store);
        var timer = new ActiveTimer(
            new TodoIdentity("/todos/work.md", 7), "Work", "Prepare proposal",
            new DateTime(2026, 8, 10, 9, 15, 0));

        var result = service.Record(timer, new DateTime(2026, 8, 10, 10, 0, 0), new TimerConfiguration("/logs"));

        result.Succeeded.Should().BeTrue();
        store.Files.Should().ContainSingle();
        store.Files.Single().Key.Should().EndWith("2026/08/Time - 33.md");
        store.Files.Single().Value.Should().Contain("# Time log · Week 33")
            .And.Contain("## Monday, 10 Aug 2026")
            .And.Contain("09:15–10:00 · 45m — Work · Prepare proposal");
    }

    [Fact]
    public void Record_splits_a_session_at_midnight()
    {
        var store = new MemoryStore();
        var service = new WeeklyTimeLogService(store);
        var timer = new ActiveTimer(
            new TodoIdentity("/todos/work.md", 7), "Work", "Release",
            new DateTime(2026, 8, 16, 23, 45, 0));

        var result = service.Record(timer, new DateTime(2026, 8, 17, 0, 15, 0), new TimerConfiguration("/logs"));

        result.Succeeded.Should().BeTrue();
        store.Files.Should().HaveCount(2);
        store.Files.Values.Should().Contain(text => text.Contains("23:45–00:00 · 15m", StringComparison.Ordinal));
        store.Files.Values.Should().Contain(text => text.Contains("00:00–00:15 · 15m", StringComparison.Ordinal));
    }

    private sealed class MemoryStore : IWeeklyTimeLogFileStore
    {
        public Dictionary<string, string> Files { get; } = [];
        public bool FileExists(string path) => Files.ContainsKey(path);
        public string ReadAllText(string path) => Files[path];
        public void WriteAllTextAtomically(string path, string contents) => Files[path] = contents;
    }
}
