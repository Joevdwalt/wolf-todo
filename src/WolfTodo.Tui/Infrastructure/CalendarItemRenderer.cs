using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Infrastructure;

public sealed class CalendarItemRenderer
{
    public string MeetingHint(PlannerSlotView slot) =>
        BrowserRenderer.MeetingHint(slot);

    public string MeetingLabel(PlannerCalendarMeeting meeting) =>
        BrowserRenderer.MeetingLabel(meeting);

    public IReadOnlyList<IRenderable> MeetingDetailLines(PlannerView view, TuiTheme theme) =>
        BrowserRenderer.PlannerMeetingDetailLines(view, theme);

    public string MeetingTimeAndDuration(PlannerCalendarMeeting meeting) =>
        BrowserRenderer.MeetingTimeAndDuration(meeting);

    public string? MeetingDescriptionPreview(string? description) =>
        BrowserRenderer.MeetingDescriptionPreview(description);

    public string AllDayKindLabel(PlannerCalendarItemKind kind) =>
        BrowserRenderer.AllDayKindLabel(kind);

    public IReadOnlyList<IRenderable> AllDayDetailLines(PlannerView view, TuiTheme theme) =>
        BrowserRenderer.PlannerAllDayDetailLines(view, theme);

    public IReadOnlyList<IRenderable> AllDayAgendaLines(
        PlannerView view,
        TuiTheme theme,
        int contentHeight) =>
        BrowserRenderer.AllDayAgendaLines(view, theme, contentHeight);
}
