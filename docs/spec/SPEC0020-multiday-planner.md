# SPEC 0020: Multiday Planner

## Status

Draft

## Purpose

Extend the Day Planner with a multiday overview while preserving the existing
single-day planner as the detailed scheduling view. The multiday planner must
continue to use Markdown todos as its only task storage model and must not
introduce a separate planning database or cross-day task representation.

## Relationship to Existing Specifications

This specification extends [SPEC0009: Day Planner](SPEC0009-day-planner.md).
The existing single-day timeline, all-day pane, task actions, calendar
integration, editor, and responsive behavior remain supported unless this
specification explicitly extends them.

[SPEC0008: Todo Scheduling Metadata](SPEC0008-todo-scheduling-metadata.md)
continues to define the persisted schedule. A task has one scheduled date,
optional start time, and optional duration. A multiday view does not make a
single task span multiple dates. Planning work across several dates requires
separate task schedules or separate tasks.

## Design

The existing single-day rendering is the visual baseline for the planner. At
an 80-column terminal, the planner uses a compact timeline and keeps the
selected summary, all-day items, and command hints within the same terminal
width. Long task and event titles are truncated with an ellipsis; they must not
wrap the timeline or increase its width.

The single-day 80-column baseline is:

```text
WOLF TODO // TODOS  [DAY PLANNER]  MODE:BROWSE  THU 03 SEP
┌──────────┬───────────────────────────────────────────────────────────────────┐
│ TIME     │ PLAN                                                              │
├──────────┼───────────────────────────────────────────────────────────────────┤
│ 06:00    │ ├▶                                                                │
│     —    │ │                                                                 │
│ 06:30    │ │                                                                 │
│     —    │ │                                                                 │
│ 08:00    │ ├─ ⬥ NEC XON Conference and Lunch with the team…                  │
│     —    │ │  ├─ ⬥ Joe social                                                │
│ 10:00    │ │  ├─ ⬥ weekly catch up                                           │
│ 10:51    │ ┣━━ NOW · 39m · Sales Sprint Planning ━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│ 11:30    │ │  ├─ ⬥ Sales Sprint Planning                                     │
│ 13:00    │ │  ├─ ⬥ Performance Review                                        │
│ 15:00    │ │  ├─ ⬥ Joe Office hours                                          │
└──────────┴───────────────────────────────────────────────────────────────────┘
┌─SELECTED─────────────────────────────────────────────────────────────────────┐
│ Empty timeslot                                                               │
└──────────────────────────────────────────────────────────────────────────────┘
┌─ALL DAY──────────────────────────────────────────────────────────────────────┐
│   ◆ Natasha Collins's birthday                                               │
└──────────────────────────────────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────────────────────────────────┐
│ TAB PANE  J/K ITEM  G/G TOP/BOTTOM  [/] DAY  T TODAY  L MOVE  / FILTER       │
│ U UNSCHEDULE  A CREATE  E EDIT  SPACE COMPLETE  V DETAILS  R CALENDAR        │
│ CALENDAR READY                                                               │
└──────────────────────────────────────────────────────────────────────────────┘
```

The compact multiday layout keeps the shared time ruler and synchronizes the
timeline window across date columns. The current-time marker belongs only to
the column for today:

```text
WOLF TODO // TODOS  [DAY PLANNER]  MODE:BROWSE  THU 03–FRI 04 SEP
┌──────────┬─────────────────────────────┬─────────────────────────────────────┐
│ TIME     │ THU 03                      │ FRI 04                             │
├──────────┼─────────────────────────────┼─────────────────────────────────────┤
│ 08:00    │ ├─ ⬥ Team planning          │                                     │
│     —    │ │  ├─ ⬥ Prepare agenda      │ ├─ ⬥ Catch up                       │
│ 10:51    │ ┣━━ NOW · 39m ━━━━━━━━━━━━━ │                                     │
│ 11:30    │                             │ ├─ ○ Charlene work · 15m            │
│ 13:00    │                             │ ├─ ⬥ Performance Review             │
└──────────┴─────────────────────────────┴─────────────────────────────────────┘
┌─SELECTED─────────────────────────────────────────────────────────────────────┐
│ THU 03 · EMPTY TIMESLOT                                                      │
└──────────────────────────────────────────────────────────────────────────────┘
┌─ALL DAY──────────────────────────────────────────────────────────────────────┐
│ THU 03  ◆ Natasha Collins's birthday                                        │
│ FRI 04  —                                                                    │
└──────────────────────────────────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────────────────────────────────┐
│ TAB PANE  J/K ITEM  [/] RANGE  +/- DAYS  S SINGLE DAY  / FILTER              │
│ A CREATE  E EDIT  U UNSCHEDULE  SPACE COMPLETE  V DETAILS  R CALENDAR        │
└──────────────────────────────────────────────────────────────────────────────┘
```

The samples establish these width and responsive rules for the new view:

- Every rendered line, including borders, headers, panels, and command hints,
  is at most 80 terminal cells wide.
- The timeline time column remains fixed at 10 cells in the compact layout;
  the remaining width is divided among date columns and their separators.
- The selected summary, all-day panel, and command panel use the full compact
  width below the timeline rather than creating horizontal overflow.
- Multiday columns divide the available width before rendering their content.
  Each date column has at least 24 terminal cells of content width. Secondary
  metadata is removed before titles are truncated; titles do not wrap or
  expand a date column.
- The selected range is a maximum of one, two, or three adjacent dates. The
  renderer automatically displays the largest number of columns that fits the
  current width and the selected maximum. At 80 columns this permits one or
  two dates, while three dates require a wider terminal.
- Dates in the selected range that are not currently rendered remain available
  through explicit range navigation. The header or range control identifies
  the complete selected range so no date disappears silently.
- The current-time marker is rendered only in today's date column. If today is
  outside the selected range, no marker is shown.
- All-day items remain associated with their dates. In the compact full-width
  panel they are grouped under an explicit date label.
- Footer commands wrap at word boundaries and their rendered height is
  reserved from the viewport budget. Command text must not be clipped.
- All date columns share one vertical timeline window. Scrolling to keep the
  selected slot visible scrolls every date column by the same amount.
- The header, selected summary, all-day panel, and wrapped command footer are
  included in the height calculation, and the final terminal row remains
  reserved according to SPEC0013.

The final design may replace the samples' exact glyph arrangement, but it must
preserve the width budget, hierarchy, selection treatment, timeline branches,
all-day date association, synchronized scrolling, and command visibility
established here.

## View Model

The Day Planner provides two views within the existing Day Planner tab:

- The single-day view is the default detailed view. It retains the current
  quarter-hour timeline and the separate all-day pane.
- The multiday view is an overview of adjacent dates. It is intended for
  comparing schedules and performing direct task actions across visible days.

The multiday view supports a selected range of one, two, or three adjacent
dates. This range is the maximum number of dates the renderer may show; the
actual number of columns is calculated from the terminal width. The selected
date range is transient application state and is not written to task Markdown
or the global project configuration.

The overview has one selected date and one selected planner item at a time.
The selected item may be a timed todo, a date-only todo, a calendar event, or
an empty scheduling destination, subject to the existing read-only rules for
external calendar items.

## Range Behavior

- The visible dates are consecutive calendar dates.
- The selected date remains within the visible range whenever the range is
  changed or navigated.
- Users can increase the selected range up to the supported maximum of three
  days or reduce it to two or one day without changing task schedules.
- The renderer shows the largest number of date columns that fits the current
  width and the selected range maximum. A date that is part of the selected
  range but not currently rendered remains reachable through range navigation.
- Moving the range earlier or later preserves its selected maximum unless the
  user changes the range size explicitly.
- When the current range cannot retain a selected item after a date or size
  change, selection falls back to the first valid item on the selected date.
- Switching back to the single-day view opens the selected date in the existing
  detailed planner.
- Switching between views preserves the selected range, selected date, focus,
  and selection where those values remain valid.

The range controls appear in the contextual command panel and use configured
planner bindings. The selected range is displayed in the header or range
control even when the current width renders fewer date columns.

## Timeline and All-Day Content

Each visible date displays the same scheduling concepts as the single-day
planner:

- quarter-hour timeline slots from 06:00 through 21:45;
- timed todo assignments;
- duration blocks spanning consecutive quarter-hour slots;
- the live current-time marker in the date column when that visible date is
  today;
- timed Google Calendar meetings and calendar events, when configured;
- a date-only all-day area for todos and read-only calendar items.

An item is rendered in the column for its scheduled or calendar date. A timed
todo without an explicit duration remains an instantaneous item. A duration is
rendered only across the slots belonging to its scheduled date; it is not
carried into the next date column. All date columns share the same vertical
timeline window, so scrolling to keep the selected slot visible scrolls every
date column by the same amount.

The compact all-day panel is full width below the timeline. It groups items by
date and prefixes each group with an explicit date label so date ownership is
not lost when the columns are no longer present. The selected summary includes
the selected date and, when applicable, the selected time and item source.

The current-time marker is rendered only in today's date column. If today is
outside the selected range, the timeline has no current-time marker.

The multiday view uses the same task state, priority, completion, schedule,
calendar, and selection styling as the single-day planner. It uses the shared
visual rules in [SPEC0013: Operational Console Design System](SPEC0013-operational-console-design-system.md).

## Navigation and Selection

The multiday view uses configured planner navigation bindings wherever they
already have a matching single-day meaning. It must provide access to:

- movement between rendered date columns and their timeline slots;
- movement between visible date-only/all-day items;
- date-range movement to earlier and later adjacent ranges;
- changing the visible range between one, two, and three days;
- switching to the single-day detail view;
- switching back to the multiday overview.

A new configurable planner-view binding is added for switching between the
single-day and multiday views. Existing bindings are not repurposed. The
default gesture is determined during implementation and documented in the
final design and configuration documentation.

When the overview has no valid task or calendar item at the current position,
the selected date and empty scheduling destination remain navigable. Empty
destinations continue to use the existing planner creation and assignment
behavior. Range navigation is also available for selected-range dates that
are not rendered as columns at the current width.

## Direct Actions

The multiday overview supports direct actions for the selected item using the
existing planner workflows:

- edit opens the shared task editor for the selected todo;
- completion toggles the selected Markdown todo;
- move changes the selected todo's scheduled date and, when applicable, time;
- unschedule clears the selected todo's schedule;
- create creates a todo using the selected date and scheduling destination;
- assignment schedules an unscheduled todo into the selected destination;
- external editing opens the selected todo's Markdown source through the
  configured external editor;
- timer and Pomodoro actions continue to target the selected todo.

Actions must target the selected item, not the first item in a date column or
the first item in an overlapping slot. Existing overlap selection behavior
continues to apply within each date.

Read-only Google Calendar items continue to expose details but reject task
mutation actions with a clear error. Calendar events do not reserve planner
destinations and do not prevent Markdown todo scheduling.

Detailed task editing remains the shared editor behavior defined by
SPEC0010 and SPEC0011. The editor continues to edit one task at a time and
does not become a multiday content editor.

## Scheduling and Persistence

The multiday view does not introduce new task fields or a new persistence
format. All mutations use the existing conflict-safe Markdown mutation
workflows.

- Scheduling a todo writes its existing date, optional time, and duration
  metadata.
- Moving a todo between visible dates changes only its scheduled date and,
  when selected, its start time.
- Moving a todo preserves its stored duration.
- Timed occupancy conflicts continue to be rejected according to SPEC0008.
- Multiple date-only todos may share an all-day destination.
- Calendar data remains read-only and is never written to project Markdown.
- Changing the visible range, selected date, or planner view never writes a
  project file.
- Failed mutations retain the current planner state and expose the existing
  error behavior.

## Calendar Integration

When Google Calendar integration is enabled, the planner loads calendar data
for every visible date. Calendar refresh and authentication failures remain
non-blocking and must not prevent use of Markdown todos.

The multiday view preserves the existing calendar rules:

- timed meetings and events are rendered in their date column;
- all-day calendar items are rendered in the appropriate all-day area;
- calendar items remain read-only;
- meetings may produce overlap warnings but never reserve todo slots;
- calendar details identify the selected event and its source where the
  existing single-day inspector provides that information.

## Responsive Behavior

The multiday view must remain usable across the supported terminal sizes.

- Wide terminals show all selected date columns and their available details
  when the minimum column width permits them.
- Medium terminals reduce secondary details and render as many selected date
  columns as the width permits, up to the selected range maximum.
- At 80 columns, one or two date columns may be rendered; three date columns
  are rendered only when each has at least 24 terminal cells of content width.
- Narrow or short terminals may temporarily show one focused date, but the
  complete selected range remains visible in the header or range control and
  every date remains reachable through explicit navigation.
- The header, active view state, selected date, range controls, and contextual
  command hints must remain reachable.
- The final terminal row remains reserved according to SPEC0013.
- A date may not silently disappear because of terminal width or height; an
  unrendered selected-range date must be indicated and navigable.
- Long task titles, project names, event titles, and metadata use the existing
  truncation rules without wrapping the timeline structure unexpectedly.
- Footer commands wrap at word boundaries and the wrapped height is included
  in the planner viewport calculation.

The design examples and rules above define the exact compact column layout,
adaptive fallback, and viewport behavior.

## Acceptance Scenarios

1. The existing single-day planner remains available and behaves as specified
   by SPEC0009.
2. Users can switch between the single-day and multiday views using the new
   configurable planner-view binding.
3. Users can display one, two, or three adjacent dates.
4. Users can add or remove a visible day without changing any task Markdown.
5. Timed todos, duration blocks, all-day todos, and calendar items appear under
   the correct visible date.
6. A selected task can be edited, completed, moved, unscheduled, externally
   edited, timed, or assigned using existing planner workflows.
7. Direct actions target the selected item even when multiple items overlap or
   multiple dates are visible.
8. Moving a task between visible dates preserves its duration and rejects
   occupied timed destinations according to existing rules.
9. Date-only tasks continue to support multiple items on the same date.
10. Google Calendar data appears for each visible date without becoming
    writable or blocking todo operations.
11. Switching views preserves valid range and selection state.
12. Narrow, medium, and wide terminals retain access to every visible date and
    all required actions.
13. Range changes and view changes do not create, migrate, or rewrite Markdown
    task storage.
14. Failed mutations leave the planner state intact and display a useful
    error.
15. At exactly 80 columns, every rendered line is no wider than 80 terminal
    cells, including borders, panels, and wrapped command hints.
16. The renderer shows the largest legible number of date columns up to the
    selected range maximum and keeps unrendered range dates navigable.
17. All date columns share one vertically synchronized timeline window.
18. The current-time marker appears only in today's column and is absent when
    today is outside the selected range.
19. Compact all-day items are grouped under explicit date labels, and the
    selected summary identifies the selected date.

## References

- [SPEC0008: Todo Scheduling Metadata](SPEC0008-todo-scheduling-metadata.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [SPEC0013: Operational Console Design System](SPEC0013-operational-console-design-system.md)
- [SPEC0015: Day Schedule Markdown Export](SPEC0015-day-schedule-markdown-export.md)
