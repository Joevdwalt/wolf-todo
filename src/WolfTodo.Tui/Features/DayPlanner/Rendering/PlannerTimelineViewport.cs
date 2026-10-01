using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed class PlannerTimelineViewport
{
    public IReadOnlyList<int> WindowPlannerMultiDaySlots(
        IReadOnlyList<PlannerDayColumnView> columns,
        int selectedSlotIndex,
        int availableRows)
    {
        var slotCount = columns.Min(column => column.Slots.Length);
        // Multiday overlaps share one horizontal row, so a busy date never
        // consumes additional vertical space in the synchronized window.
        if (slotCount <= availableRows)
        {
            return Enumerable.Range(0, slotCount).ToArray();
        }

        var selected = Math.Clamp(selectedSlotIndex, 0, slotCount - 1);
        var windowSize = Math.Max(1, availableRows);
        var start = Math.Max(0, selected - windowSize + 1);
        return Enumerable.Range(start, Math.Min(windowSize, slotCount - start)).ToArray();
    }

    public PlannerMultiDayWindow WindowPlannerMultiDayTimeline(
        IReadOnlyList<PlannerDayColumnView> columns,
        int selectedSlotIndex,
        int availableRows,
        DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);
        var todayColumn = columns.FirstOrDefault(column => column.Date == today);
        var showNow = todayColumn is not null && availableRows >= 2;
        var markerSlotIndex = todayColumn is null
            ? -1
            : Enumerable.Range(0, todayColumn.Slots.Length)
                .FirstOrDefault(index => todayColumn.Slots[index].Time >= currentTime, -1);
        if (todayColumn is not null && markerSlotIndex < 0)
        {
            markerSlotIndex = todayColumn.Slots.Length;
        }

        var slotsToRender = WindowPlannerMultiDaySlots(
            columns,
            selectedSlotIndex,
            Math.Max(1, availableRows - (showNow ? 1 : 0)));
        if (showNow && todayColumn is { } visibleTodayColumn && slotsToRender.Count > 0)
        {
            var todaySlots = visibleTodayColumn.Slots;
            var markerAnchor = Math.Clamp(markerSlotIndex, 0, todaySlots.Length - 1);
            var requiredStart = Math.Min(selectedSlotIndex, markerAnchor);
            var requiredEnd = Math.Max(selectedSlotIndex, markerAnchor);
            var slotBudget = Math.Max(1, availableRows - 1);
            if (requiredEnd - requiredStart + 1 <= slotBudget)
            {
                var start = requiredStart;
                var end = requiredEnd + 1;
                while (start > 0 && end - start < slotBudget)
                {
                    start--;
                }

                while (end < todaySlots.Length && end - start < slotBudget)
                {
                    end++;
                }

                slotsToRender = Enumerable.Range(start, end - start).ToArray();
            }
        }

        var includesNow = showNow && slotsToRender.Count > 0 &&
                          markerSlotIndex >= slotsToRender[0] &&
                          markerSlotIndex <= slotsToRender[^1] + 1;
        return new PlannerMultiDayWindow(slotsToRender, markerSlotIndex, includesNow);
    }

    public IReadOnlyList<PlannerTimelineRow> WindowPlannerTimeline(
        IReadOnlyList<PlannerSlotView> slots,
        int selectedIndex,
        int availableRows,
        DateOnly selectedDate,
        DateTime now,
        IReadOnlyList<PlannerCalendarMeeting>? meetings = null,
        PlannerFocusBlock? activeFocusBlock = null)
    {
        var rows = new List<PlannerTimelineRow>(slots.Count + 1);
        var today = DateOnly.FromDateTime(now);
        var currentTime = new TimeOnly(now.Hour, now.Minute);
        var addMarker = selectedDate == today;
        var nextMeeting = addMarker ? NextMeeting(meetings ?? [], currentTime) : null;
        TimeSpan? timeUntilNextMeeting = nextMeeting is null ? null : nextMeeting.Start - currentTime;
        TimeSpan? pomodoroRemaining = addMarker && activeFocusBlock is not null
            ? activeFocusBlock.Remaining(now)
            : null;
        var pomodoroTitle = pomodoroRemaining is not null ? activeFocusBlock?.TodoTitle : null;
        var markerAdded = false;
        foreach (var slot in slots)
        {
            if (addMarker && !markerAdded && currentTime <= slot.Time)
            {
                rows.Add(new PlannerNowTimelineRow(
                    currentTime,
                    timeUntilNextMeeting,
                    nextMeeting?.Title,
                    pomodoroRemaining,
                    pomodoroTitle));
                markerAdded = true;
            }

            rows.Add(new PlannerSlotTimelineRow(slot));
        }

        if (addMarker && !markerAdded)
        {
            rows.Add(new PlannerNowTimelineRow(
                currentTime,
                timeUntilNextMeeting,
                nextMeeting?.Title,
                pomodoroRemaining,
                pomodoroTitle));
        }

        if (PlannerTimelineHeight(rows) <= availableRows)
        {
            return rows;
        }

        var selectedRow = rows.FindIndex(row =>
            row is PlannerSlotTimelineRow slotRow && slotRow.Slot.IsSelected);
        if (selectedRow < 0)
        {
            selectedRow = Math.Clamp(selectedIndex, 0, rows.Count - 1);
        }

        var start = selectedRow;
        var usedRows = TimelineRowHeight(rows[selectedRow]);
        while (start > 0 && usedRows + TimelineRowHeight(rows[start - 1]) <= availableRows)
        {
            start--;
            usedRows += TimelineRowHeight(rows[start]);
        }

        var end = selectedRow + 1;
        while (end < rows.Count && usedRows + TimelineRowHeight(rows[end]) <= availableRows)
        {
            usedRows += TimelineRowHeight(rows[end]);
            end++;
        }

        var markerRow = rows.FindIndex(row => row is PlannerNowTimelineRow);
        if (markerRow >= 0)
        {
            var requiredStart = Math.Min(selectedRow, markerRow);
            var requiredEnd = Math.Max(selectedRow, markerRow) + 1;
            var requiredHeight = PlannerTimelineHeight(rows.Skip(requiredStart).Take(requiredEnd - requiredStart));
            if (requiredHeight <= availableRows)
            {
                start = requiredStart;
                end = requiredEnd;
                usedRows = requiredHeight;
                while (start > 0 && usedRows + TimelineRowHeight(rows[start - 1]) <= availableRows)
                {
                    start--;
                    usedRows += TimelineRowHeight(rows[start]);
                }

                while (end < rows.Count && usedRows + TimelineRowHeight(rows[end]) <= availableRows)
                {
                    usedRows += TimelineRowHeight(rows[end]);
                    end++;
                }
            }
        }

        return rows.Skip(start).Take(end - start).ToArray();
    }

    public PlannerCalendarMeeting? NextMeeting(
        IReadOnlyList<PlannerCalendarMeeting> meetings,
        TimeOnly currentTime)
    {
        var nextMeeting = meetings
            .Where(meeting => meeting.Start > currentTime)
            .OrderBy(meeting => meeting.Start)
            .ThenBy(meeting => meeting.End)
            .ThenBy(meeting => meeting.Title, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return nextMeeting;
    }

    public int PlannerTimelineHeight(IEnumerable<PlannerTimelineRow> rows) =>
        rows.Sum(TimelineRowHeight);

    public int TimelineRowHeight(PlannerTimelineRow row) => row switch
    {
        PlannerSlotTimelineRow slot => PlannerTimelineRenderModel.ForSlot(slot.Slot).Count,
        PlannerNowTimelineRow => 1,
        PlannerTimelineRow unknownRow => throw new ArgumentOutOfRangeException(
            nameof(row), unknownRow, "Unsupported planner timeline row.")
    };
}
