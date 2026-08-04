using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser.Rendering;

public sealed class TodoRowRendererTests
{
    private readonly TodoRowRenderer renderer = new();

    [Fact]
    public void FormatSchedule_includes_time_only_when_present()
    {
        renderer.FormatSchedule(new TodoSchedule(new DateOnly(2026, 8, 4), null))
            .Should().Be("2026-08-04");
        renderer.FormatSchedule(new TodoSchedule(new DateOnly(2026, 8, 4), new TimeOnly(9, 30)))
            .Should().Be("2026-08-04 09:30");
    }

    [Fact]
    public void FormatDuration_uses_minutes()
    {
        renderer.FormatDuration(TimeSpan.FromMinutes(45)).Should().Be("45m");
        renderer.FormatDuration(null).Should().BeNull();
    }

    [Fact]
    public void PriorityCode_maps_known_priorities_and_missing_priority()
    {
        renderer.PriorityCode(TodoPriority.Highest).Should().Be("!");
        renderer.PriorityCode(TodoPriority.High).Should().Be("H");
        renderer.PriorityCode(TodoPriority.Medium).Should().Be("M");
        renderer.PriorityCode(TodoPriority.Low).Should().Be("L");
        renderer.PriorityCode(TodoPriority.Lowest).Should().Be(".");
        renderer.PriorityCode(null).Should().Be("-");
    }

    [Fact]
    public void StatusGlyph_maps_completion_state()
    {
        renderer.StatusGlyph(false).Should().Be("◯");
        renderer.StatusGlyph(true).Should().Be("✓");
    }

    [Fact]
    public void Truncate_preserves_display_width_budget()
    {
        renderer.Truncate("abcdef", 4).Should().Be("abc…");
        renderer.Truncate("abc", 4).Should().Be("abc");
    }
}
