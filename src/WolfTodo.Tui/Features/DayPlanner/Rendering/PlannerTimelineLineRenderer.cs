using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed class PlannerTimelineLineRenderer
{
    private readonly SurfaceThemeRenderer themeRenderer = new();
    private readonly CalendarItemRenderer calendarItemRenderer = new();

    public IRenderable PlannerTimeRulerLine(PlannerTimelineRenderRow row, TuiTheme theme, bool selected = false)
    {
        var text = row.IsMinorTimeTick ? row.TimeTickGlyph.PadLeft(5) : row.TimeLabel.PadLeft(5);
        var selectedSlot = selected || (row.IsSelected && row.IsEmpty);
        var (color, decoration) = selectedSlot switch
        {
            true => (theme.AccentBright, Decoration.Bold),
            false when row.IsMinorTimeTick => (theme.Muted, Decoration.Dim),
            false => (theme.Date, Decoration.None)
        };
        var line = new Text(text, themeRenderer.Style(color, decoration));
        // Single-day callers apply their existing cell surface themselves.
        // Multiday passes selected explicitly because its shared ruler is a
        // separate cell from the active date pane.
        return selected ? themeRenderer.OnSurface(line, theme.Surface2, true) : line;
    }

    public IRenderable PlannerTimelineRenderLine(PlannerTimelineRenderRow row, TuiTheme theme)
    {
        var selected = row.IsSelected;
        var active = row.IsActive;
        var selectionBridge = row.IsSelectionBridge && !active;
        var completed = row.ItemType == PlannerItemType.Task &&
            row.StatusGlyph == TodoGlyphs.CompletedTodoGlyph;
        var color = row switch
        {
            { ItemType: PlannerItemType.Pomodoro } => theme.Timer,
            { IsActive: true } => theme.AccentBright,
            { ItemType: PlannerItemType.Task, StatusGlyph: TodoGlyphs.CompletedTodoGlyph } => theme.Muted,
            { ItemType: PlannerItemType.Task } => theme.Text,
            { ItemType: null } => theme.Muted,
            { ItemType: PlannerItemType.Meeting or PlannerItemType.CalendarEvent } => theme.Info,
            { ItemType: PlannerItemType unknownItemType } => throw new ArgumentOutOfRangeException(
                nameof(row.ItemType), unknownItemType, "Unsupported planner item type.")
        };
        var decoration = active switch
        {
            true => Decoration.Bold,
            false when completed || row.IsEmpty => Decoration.Dim,
            false => Decoration.None
        };
        var line = new System.Text.StringBuilder();
        if (selectionBridge)
        {
            // The overlap stack carries the selected item's vertical branch,
            // but the row itself still belongs to another item. Surface only
            // the junction character; its horizontal branch and content stay
            // in that item's ordinary styling.
            themeRenderer.AppendStyled(
                line,
                row.BranchGlyph[..1],
                theme.AccentBright,
                Decoration.Bold,
                theme.Surface2);
            themeRenderer.AppendStyled(line, row.BranchGlyph[1..], theme.BorderActive, decoration);
        }
        else
        {
            var branchHighlighted = active || selected;
            var branchColor = branchHighlighted switch
            {
                true => theme.AccentBright,
                false when row.IsEmpty => theme.Muted,
                false => theme.BorderActive
            };
            var branchDecoration = branchHighlighted switch
            {
                true => Decoration.Bold,
                false => decoration
            };
            themeRenderer.AppendStyled(line, row.BranchGlyph, branchColor, branchDecoration);
        }

        if (row.IsEmpty)
        {
            return new Markup(line.ToString()).Ellipsis();
        }

        themeRenderer.AppendStyled(line, " ", color, decoration);
        if (row.StatusGlyph.Length > 0)
        {
            var glyphColor = row switch
            {
                { ItemType: PlannerItemType.Task, StatusGlyph: TodoGlyphs.CompletedTodoGlyph } => color,
                { ItemType: PlannerItemType.Task, IsActive: true } => theme.AccentBright,
                { ItemType: PlannerItemType.Task } => theme.Accent,
                PlannerTimelineRenderRow otherRow => color
            };
            themeRenderer.AppendStyled(line, row.StatusGlyph + " ", glyphColor, decoration);
            themeRenderer.AppendStyled(line, row.Title, color, decoration);
            if (row.Metadata.Length > 0)
            {
                var metadataColor = row switch
                {
                    { ItemType: PlannerItemType.Pomodoro } => theme.Timer,
                    { IsActive: true } => theme.AccentBright,
                    PlannerTimelineRenderRow otherRow => theme.Muted
                };
                var separator = row.Title.Length == 0 ? string.Empty : " ";
                themeRenderer.AppendStyled(line, separator + row.Metadata, metadataColor, decoration);
            }
        }

        // Date panes have fixed widths. Ellipsizing here prevents a long task
        // or calendar title from becoming a second physical table row.
        return new Markup(line.ToString()).Ellipsis();
    }

    public IRenderable PlannerMeetingLine(
        PlannerCalendarMeeting meeting,
        string prefix,
        bool selected,
        int additionalMeetings,
        TuiTheme theme)
    {
        var line = new System.Text.StringBuilder();
        var color = selected ? theme.AccentBright : theme.Info;
        var decoration = selected ? Decoration.Bold : Decoration.None;
        themeRenderer.AppendStyled(line, $"{prefix} MEETING ", color, decoration);
        themeRenderer.AppendStyled(line, calendarItemRenderer.MeetingLabel(meeting), color, decoration);
        if (additionalMeetings > 0)
        {
            themeRenderer.AppendStyled(line, $" +{additionalMeetings}", selected ? theme.AccentBright : theme.Warning, decoration);
        }

        return new Markup(line.ToString());
    }
}
