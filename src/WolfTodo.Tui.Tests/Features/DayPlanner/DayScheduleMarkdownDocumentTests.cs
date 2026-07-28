using FluentAssertions;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class DayScheduleMarkdownDocumentTests
{
    [Fact]
    public void ReplaceDaySection_replaces_only_the_matching_day_section()
    {
        var date = new DateOnly(2026, 7, 13);
        var contents = "# 📅 Sunday, 12 Jul 2026\nold\n\n# 📅 Monday, 13 Jul 2026\nstale\n\n# 📅 Tuesday, 14 Jul 2026\nkeep\n";

        var result = DayScheduleMarkdownDocument.ReplaceDaySection(contents, date, "# 📅 Monday, 13 Jul 2026\nnew");

        result.Should().Be("# 📅 Sunday, 12 Jul 2026\nold\n\n# 📅 Monday, 13 Jul 2026\nnew\n\n# 📅 Tuesday, 14 Jul 2026\nkeep\n");
    }

    [Fact]
    public void ReplaceDaySection_appends_missing_day_to_existing_note()
    {
        var result = DayScheduleMarkdownDocument.ReplaceDaySection(
            "# Weekly notes\nKeep this",
            new DateOnly(2026, 7, 13),
            "# 📅 Monday, 13 Jul 2026\nnew");

        result.Should().Be("# Weekly notes\nKeep this\n\n# 📅 Monday, 13 Jul 2026\nnew\n");
    }
}
