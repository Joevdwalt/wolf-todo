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
columns share the remaining width. Allocate column
width before rendering content. Remove secondary metadata, then truncate long
titles with an ellipsis; content never wraps or widens a date column.

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
duration blocks stop at the end of their scheduled date.

### Current-time marker

Show the live current-time marker only in today's column. Show no marker when
today is outside the selected range.

### All-day content

Each date has its own all-day area directly below its timeline. Date-only todos
and read-only calendar items stay under their owning date. Only the active
date's all-day item can be selected; other dates remain visually quiet. The
selected summary shows the date and, when relevant, time and item source.

### Overlapping items

Apply the [SPEC0009](SPEC0009-day-planner.md) overlap rules independently in
each date column. Each quarter-hour row has one cell per date; simultaneous
items divide that cell into equal left-to-right segments. Titles shrink to
identifying glyphs and never wrap. A busy date adds neither slot height nor
padding to other columns. Duration segments retain start, continuation, end,
and active styling, with widths calculated per slot.

### Overflow selection

When minimum segment widths cannot show every item, the final segment shows
that date's `+N` count. Each date has its own visible item window. `j` and `k`
traverse every item in stable order; selecting a hidden item shifts only the
active date's window and updates the Inspector.

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

These wireframes show structure and interaction; exact glyphs and colors may
follow the shared planner design system. Rows between the examples are omitted.

### Two columns at 80 cells

The selected range contains three dates, while the terminal fits two. The
header exposes the full range; `l` can reach Saturday. `▶` marks the active
date and item, and the current-time marker appears only under today.

```text
WOLF TODO / DAY PLANNER / MULTIDAY       THU 03–SAT 05 SEP (2 OF 3)
┌──────────┬───────────────────────────────┬───────────────────────────────┐
│TIME      │▶ THU 03                       │FRI 04                         │
├──────────┼───────────────────────────────┼───────────────────────────────┤
│08:00     │  ○ Plan sprint                │  ⬥ Team sync                  │
│09:30     │▶ ○ Write brief ┊ ⬥ Review     │  ○ Call client                │
│10:51     │  ┣━━ NOW · 39m                │                               │
│11:30     │  │                            │  ○ Follow up                  │
│ALL DAY   │  ◆ Birthday                   │  —                            │
└──────────┴───────────────────────────────┴───────────────────────────────┘
┌─SELECTED─────────────────────────────────────────────────────────────────┐
│THU 03 · 09:30 · Write brief (todo)                                       │
└──────────────────────────────────────────────────────────────────────────┘
┌─COMMANDS─────────────────────────────────────────────────────────────────┐
│H/L DATE  J/K ITEM  TAB PANE  +/- DAYS  S SINGLE DAY                      │
│ENTER MOVE  A CREATE  E EDIT  U UNSCHEDULE  / FILTER                      │
└──────────────────────────────────────────────────────────────────────────┘
```

### Three columns on a wide terminal

All range dates are visible. The active column owns selection. The `+2` item
can be reached with `j`/`k` without enlarging Friday's row.

```text
WOLF TODO / DAY PLANNER / MULTIDAY / THU 03–SAT 05 SEP
┌──────────┬────────────────────────────┬────────────────────────────┬────────────────────────────┐
│TIME      │  THU 03                    │ [FRI 04]                   │  SAT 05                    │
├──────────┼────────────────────────────┼────────────────────────────┼────────────────────────────┤
│09:30     │  ○ Write brief             │▶ ○ Call      ┊ ⬥ Review    │  ○ Draft                   │
│10:00     │  │                         │  ○ Follow up ┊ +2          │  │                         │
│ALL DAY   │  ◆ Birthday                │  —                         │  ◆ Conference              │
└──────────┴────────────────────────────┴────────────────────────────┴────────────────────────────┘
SELECTED: FRI 04 · 09:30 · Call (todo)
```

### Two columns on a tall terminal

A tall terminal expands the shared timeline window while keeping the selected
summary, all-day row, and commands below it. Both dates scroll together, and
the selected slot stays aligned across columns.

```text
WOLF TODO / DAY PLANNER / MULTIDAY       THU 03–SAT 05 SEP (2 OF 3)
┌──────────┬───────────────────────────────┬───────────────────────────────┐
│TIME      │  THU 03                       │ [FRI 04]                      │
├──────────┼───────────────────────────────┼───────────────────────────────┤
│06:00     │  │                            │  │                            │
│07:00     │  ○ Review inbox               │  │                            │
│08:00     │  ○ Plan sprint                │  ⬥ Team sync                  │
│09:00     │  │                            │  │                            │
│09:30     │  ○ Write brief ┊ ⬥ Review     │▶ ○ Call client                │
│10:00     │  │                            │  ○ Follow up                  │
│10:51     │  ┣━━ NOW · 39m                │  │                            │
│11:30     │  │                            │  ⬥ Project review             │
│12:00     │  ○ Lunch                      │  │                            │
│13:30     │  ⬥ Planning session           │  ○ Draft proposal             │
│15:00     │  │                            │  ⬥ Office hours               │
│16:30     │  ○ Send notes                 │  │                            │
│17:00     │  │                            │  │                            │
├──────────┼───────────────────────────────┼───────────────────────────────┤
│ALL DAY   │  ◆ Birthday                   │  —                            │
└──────────┴───────────────────────────────┴───────────────────────────────┘
┌─SELECTED─────────────────────────────────────────────────────────────────┐
│FRI 04 · 09:30 · Call client (todo)                                       │
└──────────────────────────────────────────────────────────────────────────┘
┌─COMMANDS─────────────────────────────────────────────────────────────────┐
│H/L DATE  J/K ITEM  TAB PANE  +/- DAYS  S SINGLE DAY                      │
│ENTER MOVE  A CREATE  E EDIT  U UNSCHEDULE  / FILTER                      │
└──────────────────────────────────────────────────────────────────────────┘
```

### One column on a narrow terminal

The range and active date remain visible. `h`/`l` reveal the other dates; `s`
opens Friday in the single-day detail view.

```text
DAY PLANNER / THU 03–SAT 05 SEP / FRI 04 (2/3)
┌──────────┬───────────────────────────────┐
│TIME      │▶ FRI 04                       │
├──────────┼───────────────────────────────┤
│09:30     │▶ ○ Call      ┊  ⬥ Review      │
│10:00     │  ○ Follow up ┊  +2            │
│ALL DAY   │  —                            │
└──────────┴───────────────────────────────┘
SELECTED: FRI 04 · 09:30 · Call
H/L DATE  J/K ITEM  TAB PANE  S DETAIL
```

### Moving a task between dates

```text
THU 03 · 09:30 · Write brief  → Enter: MOVE
FRI 04 · 09:30                → l: choose Friday
FRI 04 · ALL DAY              → Tab: choose all-day instead
FRI 04 · ALL DAY              → Enter: save; Escape: cancel
```

## References

- [SPEC0008: Todo Scheduling Metadata](SPEC0008-todo-scheduling-metadata.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [SPEC0011: Structured Todo Content Editor](SPEC0011-structured-todo-content-editor.md)
- [SPEC0013: Operational Console Design System](SPEC0013-operational-console-design-system.md)
