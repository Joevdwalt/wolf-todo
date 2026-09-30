# SPEC 0020: Multiday Planner

## Status

Draft

## Purpose

Add a multiday overview to Day Planner while keeping its single-day detail view
and Markdown-file task storage.

## Rules

### Existing planner behavior

The overview extends [SPEC0009](SPEC0009-day-planner.md). Its task actions,
calendar behavior, editor, and visual styling follow the single-day planner
unless a rule below changes them.

### Views and state

Day Planner opens in single-day view. The overview compares adjacent dates and
supports direct actions. View, range, focus, and selection are transient; they
never change task Markdown by themselves. Switching views retains valid state
and opens the selected date in single-day view.

### Selected range

The user chooses a maximum of one, two, or three consecutive dates. Changing
range size leaves task schedules unchanged. Moving the range earlier or later
keeps its size. The selected date stays in range; if its item becomes invalid,
selection falls back to the first valid item on that date.

### Visible columns

Render as many selected dates as fit, up to the range maximum. Each date column
needs at least 24 content cells. At 80 terminal columns, at most two dates fit;
three require a wider terminal. The header or range control identifies the
complete range, and unrendered dates remain reachable by range navigation.

### Compact width

The existing single-day layout is the visual baseline. Every rendered line,
including borders and command hints, fits within 80 terminal cells at an
80-column width. The compact timeline uses a fixed 10-cell time column; date
columns share the remaining width. Allocate column width before rendering
content. Remove secondary metadata, then truncate titles with an ellipsis;
content never wraps or widens a date column.

### Vertical layout

Date columns share one timeline window. Scrolling to keep the selected slot
visible scrolls every column equally. The selected summary, all-day area, and
command footer fit below the timeline within the terminal width. Include their
heights and the header in the viewport calculation. Wrap footer commands at
word boundaries, without clipping them. Reserve the final terminal row as
specified by [SPEC0013](SPEC0013-operational-console-design-system.md).

### Timeline content

Each visible date shows quarter-hour slots from 06:00 through 21:45, timed
todos, duration blocks, and configured calendar items. Items appear only under
their scheduled date. A todo without an explicit duration is instantaneous;
duration blocks stop at the end of their scheduled date. Each date uses one
timeline guide: item glyphs occupy its lane, and `│` connects the rows between.
The guide remains visible on empty slots and after an item ends; it does not
by itself mean an item continues.

### Duration finish cues

Show `→│HH:mm` in the last occupied slot of each timed todo, calendar item,
and active Pomodoro. The time is the item's actual end, including ends between
quarter-hour ticks. A one-slot item shows its title and finish cue on the same
row. Instantaneous todos have no cue. A finish cue never adds a physical row.
Format the selected timed item as `DATE · START–END · DURATION · TITLE (SOURCE)`;
preserve the timing before truncating its title or source on narrow terminals.

If an item continues beyond the 21:45 slot, show no false finish cue at the
bottom of the timeline. Its selected summary shows its actual end and duration,
including the next date when applicable; its block does not continue into the
next date's column. The selected summary also shows the full start, end, and
duration when an item's finish is outside the scrolled window or hidden by
horizontal overflow.

### Current-time marker

Show the live current-time marker only in today's column. Show no marker when
today is outside the selected range.

### All-day content

Each date has its own all-day area directly below its timeline. Date-only todos
and read-only calendar items stay under their owning date. Only the active
date's all-day item can be selected; other dates remain visually quiet. The
selected summary shows the date and, when relevant, time and item source.
For an all-day todo, it also shows any explicit duration as an estimate;
all-day calendar items have no timeline finish cue.

### Overlapping items

Apply the [SPEC0009](SPEC0009-day-planner.md) overlap rules independently in
each date column. Each quarter-hour row has one cell per date; simultaneous
items sit left to right at their natural widths, leaving spare space at the
right. While durations overlap, each item has a temporary path in stable
display order: `│` for continuation and `→│HH:mm` in its final occupied slot.
Keep a continuing item's path identifiable until it ends; when one remains,
collapse it onto the date guide on the next slot. An ending item and a new
start may share one row. Tight cells shrink titles before finish cues, then
fall back to identifying glyphs without wrapping. A busy date adds neither
slot height nor padding to other columns. The selected item's path retains
active styling through its occupied slots.

### Overflow selection

When minimum segment widths cannot show every item, the final segment shows
that date's `+N` count. Each date has its own visible item window. `j` and `k`
traverse every item in stable order; selecting a hidden item shifts only the
active date's window and updates the selected summary. The selected item's
glyph and finish cue take priority over its title; if even the cue cannot fit,
the selected summary remains the complete source of timing information.

### Active-date styling

Mark the active date in its header and all-day group. Its selected timeline
slot also styles the time-ruler cell and selected item. Selection styling does
not spill into other dates.

### Date-local selection

Each date retains its timeline slot, overlapping-item identity, all-day index,
and focused pane for the session. The overview has one active date and one
selected item at a time: a todo, calendar item, or empty destination. Restore
an overlapping-item selection only when returning to its original slot.

### Column navigation

`h` and `l` move the active date one column at a time, retaining the timeline
slot and focused pane. Moving beyond the range loads the adjacent date and
shifts the window one day. In single-day view, `h` and `l` keep their existing
meaning. The previous-day and next-day bindings remain single-day controls.

### Pane and item navigation

Configured pane bindings (Tab and Shift+Tab by default) switch between the
active date's timeline and all-day area. The destination restores its valid
selection or selects its first item or empty destination. `j` and `k` navigate
items and slots. Empty destinations remain navigable for creation and
assignment.

### View and range bindings

The new configurable `planner_toggle_view` binding defaults to `s`.
`planner_increase_range` and `planner_decrease_range` default to `+` and `-`
and adjust the maximum between one and three dates. The configurable
`planner_previous_column` and `planner_next_column` bindings default to `h`
and `l` and apply only in multiday view. Show range controls in the contextual
command panel.

### Move mode

During a move, `h` and `l` change date panes without leaving Move mode.
Enter confirms the active date and selected time, or an all-day destination
when that pane has focus. Escape cancels the move. Outside Move mode, Enter
opens an empty destination or starts moving the selected todo.

### Direct task actions

The selected todo supports edit, completion, move, unschedule, external
Markdown editing, timer, and Pomodoro workflows. An empty destination supports
creation and assignment. Actions target the selected item even in a crowded
slot. The shared editor still edits one task at a time under
[SPEC0010](SPEC0010-writable-todo-workflows.md) and
[SPEC0011](SPEC0011-structured-todo-content-editor.md).

### Markdown scheduling

Use the existing conflict-safe Markdown mutations and schedule fields from
[SPEC0008](SPEC0008-todo-scheduling-metadata.md): one date, optional start
time, and optional duration per task. A task never spans dates. Moving dates
changes its scheduled date and, when selected, its start time; it preserves
duration. Timed conflicts are rejected. Multiple date-only todos may share an
all-day destination. Failed mutations retain planner state and show the
existing error.

### Calendar items

Load calendar data for every visible date. Timed and all-day items appear
under their dates and expose details. They remain read-only; task mutations
show a clear error. Meetings may warn about overlaps but never reserve todo
slots. Authentication or refresh failures do not block Markdown todos.

### Small terminals

Reduce secondary details as width shrinks. Narrow or short terminals may show
one focused date, provided the complete range, selected date, view state,
range controls, and contextual commands remain accessible. Every unrendered
date remains indicated and navigable. Truncate titles and metadata without
wrapping the timeline.

## UX Designs

These wireframes show behavior; exact colors follow the shared design system.
Rows omitted between examples do not imply a different slot height. The arrow
marks an item's actual finish on its last occupied row, while a plain `│` can
be the date guide alone.

### Two columns at 80 cells

The selected range contains three dates, but two fit. The selected summary
retains the item's full time range if its finish row scrolls out of view. The
current-time marker appears only under today.

```text
WOLF TODO / DAY PLANNER / MULTIDAY       THU 03–SAT 05 SEP (2 OF 3)
┌──────────┬───────────────────────────────┬───────────────────────────────┐
│TIME      │▶ THU 03                       │FRI 04                         │
├──────────┼───────────────────────────────┼───────────────────────────────┤
│09:30     │▶ ○ Write brief ┊  ⬥ Review    │  ⬥ Team sync                  │
│09:45     │  │             ┊  │           │  │                            │
│10:00     │  │             ┊ →│10:15      │ →│10:15                       │
│10:15     │ →│10:30                       │  ○ Follow up                  │
│10:30     │  │                            │ →│10:45                       │
│10:51     │  ┣━━ NOW · 39m                │  │                            │
├──────────┼───────────────────────────────┼───────────────────────────────┤
│ALL DAY   │  ◆ Birthday                   │  ○ Send report                │
└──────────┴───────────────────────────────┴───────────────────────────────┘
┌─SELECTED─────────────────────────────────────────────────────────────────┐
│THU 03 · 09:30–10:30 · 60m · Write brief (todo)                            │
└──────────────────────────────────────────────────────────────────────────┘
┌─COMMANDS─────────────────────────────────────────────────────────────────┐
│H/L DATE  J/K ITEM  TAB PANE  +/- DAYS  S SINGLE DAY                      │
│ENTER MOVE  A CREATE  E EDIT  U UNSCHEDULE  / FILTER                      │
└──────────────────────────────────────────────────────────────────────────┘
```

Review's temporary path ends on the 10:00 row. Write brief then occupies the
single guide and ends on the 10:15 row. The plain guide on 10:30 is not part
of Write brief.

### Other interval shapes

The left labels below are slot starts, not item end times. Each example uses
one physical row per slot.

```text
ONE SLOT                 SAME FINISH               END AND NEW START
09:30  ○ Call →│09:45    09:30   ○ Draft ┊  ⬥ Sync   10:00  →│10:15 ┊ ○ Next
                         09:45   │       ┊  │
                         10:00  →│10:15  ┊ →│10:15

BETWEEN TICKS            INSTANTANEOUS             EMPTY
09:30  ⬥ Sync            09:30  ○ Send email        09:30  │
09:45  │
10:00  →│10:10
SELECTED: 09:40–10:10 · 30m · Sync (calendar)
```

An item starting during another duration gets a new temporary path. Paths
retain their item identity through a crowded interval; when one remains, it
returns to the date guide on the following slot. A 15-minute item places title
and finish in its single row. A todo without an explicit duration has no
finish cue.

### Crowded slots and selection

Items use their existing stable navigation order. The active date alone shifts
its visible path window to show the selected item; `+N` counts the items hidden
on either side. The selected item's glyph and finish cue take priority over
its title, and its complete timing remains in SELECTED.

```text
09:30  ▶ ○ Write… ┊  ⬥ Review ┊ +2
09:45    │        ┊  │        ┊ +2
10:00    │        ┊ →│10:15   ┊ +2
SELECTED: 09:30–10:30 · 60m · Write brief (todo)
```

If several items finish in the same slot, each visible item gets its own
`→│HH:mm` segment directly beneath its item glyph. A lone item returns its
finish cue to the shared date guide. An ending item and a new start also share
the row; neither adds height. On a tight segment, omit the finish cue only when
the selection marker, item glyph, and cue cannot all fit.

### Wide and narrow terminals

A wide terminal may show all three dates; each date computes its own overlap
paths and `+N` window. A narrow terminal may show only the active date, but
the full range and date position stay visible. `h`/`l` expose hidden dates and
`s` opens the active date in single-day view.

```text
WOLF TODO / DAY PLANNER / MULTIDAY / THU 03–SAT 05 SEP
┌──────────┬────────────────────────────┬────────────────────────────┬────────────────────────────┐
│TIME      │  THU 03                    │▶ FRI 04                    │  SAT 05                    │
├──────────┼────────────────────────────┼────────────────────────────┼────────────────────────────┤
│09:30     │  ○ Write brief             │▶ ○ Call ┊ ⬥ Review         │  ○ Draft                   │
│09:45     │  │                         │  │      ┊ │                │  │                         │
│10:00     │ →│10:15                    │ →│10:15 ┊ +2               │  │                         │
│10:15     │  │                         │  │                         │ →│10:30                    │
│ALL DAY   │  ◆ Birthday                │  —                         │  ◆ Conference              │
└──────────┴────────────────────────────┴────────────────────────────┴────────────────────────────┘
SELECTED: FRI 04 · 09:30–10:15 · 45m · Call (todo)
```

```text
DAY PLANNER / THU 03–SAT 05 SEP / FRI 04 (2/3)
┌──────────┬───────────────────────────────┐
│TIME      │▶ FRI 04                       │
├──────────┼───────────────────────────────┤
│09:30     │▶ ○ Call      ┊  ⬥ Review      │
│09:45     │  │           ┊  │             │
│10:00     │ →│10:15      ┊ →│10:15        │
│ALL DAY   │  —                            │
└──────────┴───────────────────────────────┘
SELECTED: FRI 04 · 09:30–10:15 · 45m · Call
H/L DATE  J/K ITEM  TAB PANE  S DETAIL
```

Short terminals scroll all visible dates together. Scrolling a finish row
out of view does not remove its start, end, or duration from SELECTED. The NOW
row stays separate from interval paths and appears only in today's column.

### End of the visible day and all-day items

An interval that continues past 22:00 has no finish cue in the 21:45 row.
SELECTED shows its actual finish and duration, with the next date when needed.
Its visible block stops under its scheduled date; the next date's column does
not gain a continuation. All-day todos and calendar items stay in their own
date's ALL DAY area. An explicit all-day todo duration appears as an estimate
in SELECTED, while an all-day calendar item has no timeline finish cue.

### Moving and editing

```text
THU 03 · 09:30–10:30 · Write brief  → Enter: MOVE
FRI 04 · 09:30                     → l: choose Friday
FRI 04 · ALL DAY                   → Tab: choose all-day instead
FRI 04 · ALL DAY                   → Enter: save; Escape: cancel
```

Moving or editing a todo recalculates its path and finish cue. Calendar items
remain read-only. Actions still target the selected item when its path is
behind `+N`.

## References

- [SPEC0008: Todo Scheduling Metadata](SPEC0008-todo-scheduling-metadata.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [SPEC0011: Structured Todo Content Editor](SPEC0011-structured-todo-content-editor.md)
- [SPEC0013: Operational Console Design System](SPEC0013-operational-console-design-system.md)
