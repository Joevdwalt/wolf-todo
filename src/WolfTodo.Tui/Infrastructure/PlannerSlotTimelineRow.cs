using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Infrastructure;

public sealed record PlannerSlotTimelineRow(PlannerSlotView Slot) : PlannerTimelineRow;
