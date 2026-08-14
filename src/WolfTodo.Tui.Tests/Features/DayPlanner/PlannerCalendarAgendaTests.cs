using FluentAssertions;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Tests.Features.DayPlanner;

public sealed class PlannerCalendarAgendaTests
{
    [Fact]
    public void Static_agendas_have_the_expected_state()
    {
        PlannerCalendarAgenda.Disabled.SyncState.Should().Be(PlannerCalendarSyncState.Disabled);
        PlannerCalendarAgenda.Syncing.SyncState.Should().Be(PlannerCalendarSyncState.Syncing);
        PlannerCalendarAgenda.Disabled.Warning.Should().BeNull();
    }
}
