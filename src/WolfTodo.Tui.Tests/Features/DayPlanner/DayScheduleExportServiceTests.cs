using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class DayScheduleExportServiceTests
{
    [Fact]
    public void Export_replaces_matching_section_and_reports_target_path()
    {
        var store = new FakeStore { Contents = "# 📅 Monday, 13 Jul 2026\nstale\n" };
        var service = new DayScheduleExportService(new DayScheduleMarkdownRenderer(), store);
        var view = new PlannerView(PlannerState.CreateInitial(new DateOnly(2026, 7, 13)), [], [], []);

        var result = service.Export(view, new DayScheduleExportConfiguration("/notes", []));

        result.Succeeded.Should().BeTrue();
        result.Path.Should().Be(Path.Combine("/notes", "2026", "07", "Week - 29.md"));
        store.Contents.Should().Contain("## Time blocks").And.NotContain("stale");
    }

    [Fact]
    public void Export_reports_missing_configuration()
    {
        var result = new DayScheduleExportService(new DayScheduleMarkdownRenderer(), new FakeStore())
            .Export(new PlannerView(PlannerState.CreateInitial(new DateOnly(2026, 7, 13)), [], [], []), null);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("[planner.export]");
    }

    private sealed class FakeStore : IDayScheduleMarkdownFileStore
    {
        public string Contents { get; set; } = string.Empty;

        public bool FileExists(string path) => Contents.Length > 0;

        public string ReadAllText(string path) => Contents;

        public void WriteAllTextAtomically(string path, string contents) => Contents = contents;
    }
}
