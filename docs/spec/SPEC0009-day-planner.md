# SPEC 0009: Day Planner

## Status

Accepted

## Purpose

Add a second application tab that assigns Markdown todos to a dated quarter-hour
agenda without introducing separate planner storage.

## Behavior

Show 64 slots from 06:00 through 21:45 for the selected date. Render the `:00`
and `:15` task slots stacked under each 30-minute time label, so the time column
shows only `:00` and `:30` labels. Start on today
each launch and preserve planner state while switching tabs. Keep the selected
slot visible on short terminals and show scheduled completed todos dimly.

Show a separate `ALL DAY` pane containing date-only Wolf Todo schedules and,
when configured, Google Calendar all-day events, focus time, and out-of-office
entries. `Tab`/`Shift+Tab` use the configured pane bindings to move focus
between the timeline and all-day pane. Within the focused pane, `j`/`k` and
`g`/`G` navigate items. Show an insertion target when the all-day pane is empty,
including on narrow terminals.

Enter on an empty all-day pane opens the unscheduled-todo picker. On a
selected all-day todo it starts move mode; `/` always opens the filtered picker
so multiple date-only todos can be assigned. `a` creates a task requiring a
date but not a time. Edit, external edit, completion, and unscheduling work as
they do for timeline todos. Move mode may switch panes: an all-day destination
writes a date-only schedule, while a timeline destination writes date and time.
Moving between them preserves duration and never performs an occupancy check
for the date-only destination.

Google Calendar all-day items are selectable and expose available type,
location, attendee, and description details in the Inspector. They are
read-only; mutation actions show a clear error.

An optional, read-only Google Calendar overlay may display timed meetings from
the primary calendar and configured additional calendar IDs as duration blocks
spanning their overlapping slots. It uses desktop OAuth configured by an
absolute client JSON path, stores refresh credentials in application state, and
refreshes with the configured `r` binding. Meetings produce overlap warnings
only; they never reserve slots or block todo assignment. An unavailable
additional calendar leaves successfully loaded calendar events visible and
identifies the failed calendar in the planner status. The planner shows syncing,
sign-in, configuration, and offline states without making the planner unusable.

Cache agendas on local disk in application state, scoped to the configured OAuth
client path and calendar IDs. On startup, show cached entries immediately and
refresh today plus seven days on either side in the background. Advance this
window when the local date changes. `r` and valid config reloads refresh the
window without clearing matching cached entries; dates outside it load on demand.
Successful day loads replace cached entries, including deleted events. Failed
day loads retain previous entries. Missing or damaged caches fall back to live
sync; persist only dates within the rolling window.

On today, add a logical current-time row immediately before the next quarter-hour
slot. Show the exact `HH:mm` value in the time column and fill the plan column
with `▶` followed by `─` characters in the configured `now` color. When the
current day's loaded calendar agenda has a later timed event, append its compact
duration followed by its title. Count every timed calendar event,
including events without attendees, but exclude scheduled todos. Skip events
that have already started, do not look beyond the current day, and retain the
plain current-time row when no later event is available. The duration retains
priority on narrow terminals; truncate long titles with an ellipsis. Fill the
actual rendered cell width without wrapping. This row is timeline content: do
not use panel borders, active-row backgrounds, intersections, or surface fills. Before 06:00 place it before the first slot; after
21:45 place it after the last. Keep the selected slot visible when it cannot fit
with the marker. While Planner is active, an idle one-minute input timeout
redraws without changing application state so the line remains current.

Enter on an empty slot opens a filterable picker of all open,
unscheduled todos from valid projects. Show several candidates at once, keep
the selection visible while scrolling, and update the list while filter input
changes. The same action on an occupied slot starts move mode. `u` unschedules,
`[`/`]` change dates, `g`/`G` jump to the first/final timeline slots, and `T`
returns to today. `/` opens the same picker with its filter active and permits
intentional overlapping work. Esc or `h` cancels modal work.

Show details for the selected assignment by default. Wide terminals place an
`INSPECTOR` beside the timeline; narrower terminals show a compact `SELECTED`
summary beneath it. Inspector content wraps within its physical row budget;
overflow is clipped at the bottom and marked with an ellipsis. Timeline
assignments show compact state and priority before their title. `v` hides or
restores only the Inspector for the current session;
the functional all-day pane remains accessible. When multiple timed items
overlap in the selected timeline slot, `j` and `k` select the next or prior item
in the planner's stable display order. At either end, they continue to the
adjacent timeline slot. `Tab` and `Shift+Tab` remain pane controls; they do not
cycle overlapping items. The Inspector shows the selected item and its position
in the slot, so supported actions target that item even when it was initially
hidden by horizontal overflow.

```text
[TIMELINE ACTIVE] ── Tab ──▶ [ALL DAY ACTIVE]
[TIMELINE ACTIVE] ◀─ Shift+Tab ─ [ALL DAY ACTIVE]
```

When a meeting-only slot is selected, the Inspector shows its title, time range,
duration, location, attendees, and a short description preview. Concurrent
meetings participate in the same horizontal layout and item navigation as other
timed items. A selected todo that overlaps a meeting retains its todo Inspector
and shows a compact Calendar conflict field.

On an occupied slot, `e` edits fields including scheduled date and time, `E` edits notes and subtasks, Ctrl+E
opens the Markdown source in `$EDITOR`, and Space toggles completion without
removing the schedule. `a` on an empty slot uses the same complete field form
as the Todos tab, pre-fills the selected date and time, and requires both values.
Creation from the all-day pane pre-fills only the required date.
After rescheduling through the field editor, Planner follows the todo to its new
date and slot. Clearing both schedule fields on an existing todo unschedules it.

All actions use configured bindings. Picker, move, and create input capture
keystrokes before application-tab switching. Intentional overlapping todo
assignments remain editable after selecting one with `j` or `k`.

### Overlapping Timed Items

When multiple timed items occupy the same quarter-hour slot, render them
left-to-right in one physical plan row. Todos, meetings, calendar events,
Pomodoros, and duration continuations use the same horizontal grammar and the
planner's stable display order. The time label or minor-tick marker appears once
for the slot. Overlaps never add timeline rows or wrap titles.

Use `┊` to separate equal-width item segments inside the plan cell. `▶` marks
the selected item; `○`, `✓`, `⬥`, and `◷` identify an open todo, completed
todo, calendar item, and Pomodoro. Duration segments retain `├`, `│`, and `└`
for start, continuation, and end. Remove secondary metadata first as segments
contract, then ellipsize titles, and finally show only the selection, interval,
and item glyphs. The stored title is unchanged.

```text
│ 12:30    │ ▶├ ○ Prepare… ┊ ├ ⬥ Client… ┊ ├ ◷ Focus… │
│     —    │  │             ┊ └            ┊ └          │
```

Visible items divide the available plan width equally:

```text
Wide:    │ ○ Prepare presentation ┊ ⬥ Client review       │
Medium:  │ ○ Prepare…            ┊ ⬥ Client…              │
Tight:   │ ○                     ┊ ⬥                      │
```

When every item cannot fit at its minimum representation, reserve the final
equal-width segment for `+N`, where `N` is the total number of items omitted
from that row. Overflow is visual only: omitted items remain in planner state,
navigation, the Inspector, and supported actions.

```text
│ 12:30    │ ▶○ Prepare… ┊ ⬥ Review… ┊ +3                 │
```

`j` and `k` traverse every item in stable order. When selection reaches an
omitted item, shift the contiguous visible item window to include it and update
`+N`; the overflow count includes omitted items on either side of that window.
The selected item always remains visible.

```text
Before j: │ ○ Prepare… ┊ ▶⬥ Review… ┊ +3                 │
After j:  │ ⬥ Review…  ┊ ▶◷ Focus…  ┊ +3                 │
```

The Inspector identifies the selected item's logical position and shows its
complete details. Todo actions target the selected todo; calendar items and
Pomodoros retain their read-only behavior.

```text
┌─INSPECTOR───────────────────────────────────────────────┐
│ ITEM 3 OF 5 · POMODORO                                  │
│ Focus session                                           │
│ 12:30–13:00 · 30m                                       │
└─────────────────────────────────────────────────────────┘
```

An active duration is highlighted in every slot it occupies, without selecting
concurrent items. Each slot computes its equal-width segments independently.
The current-time marker remains a separate, unsegmented timeline row:

```text
│ 12:51    │ ┣━━ NOW · 39m · Sales planning ━━━━━━━━━━━━━ │
│ 13:00    │ ○ Prepare… ┊ ⬥ Review… ┊ +2                  │
```

When no planner modal is active, the configured command launcher opens global
command mode. Quit, completed visibility, cancellation, and unknown-command
feedback match the Todos view.

## Acceptance Scenarios

1. The tab shell switches between Todos and Day Planner without losing either
   feature's in-process state.
2. Assignment, movement, unscheduling, filtering, and direct creation update
   the source Markdown and reload the catalog.
3. Completed assignments remain visible but cannot be selected as unscheduled
   work.
4. Wide, narrow, and short terminals retain access to slots and status hints.
5. Wrapped status hints reduce the visible slot window instead of scrolling the
   tab strip off the top of the terminal.
6. Full and compact details, multi-row picking, and editor forms remain usable
   without exceeding the terminal viewport.
7. Planner property, content, completion, and external-editor actions use the
   same conflict-safe Markdown workflows as the Todos tab.
8. Today's current-time row uses its dedicated `now` foreground without
   resembling a table border, stays within the viewport budget, advances while
   idle, and shows the duration and name of the next timed calendar event
   without considering todos.
9. Date-only schedules and calendar all-day items remain navigable in their
   separate pane without removing access to the selected timeline slot.
10. Calendar overlap warnings and failed calendar refreshes never block normal
    Markdown todo scheduling.
11. Todos move between timed and all-day destinations without losing duration,
    and multiple todos may share one all-day destination.
12. Mixed timed items share one non-wrapping row, divide its width equally, and
    progressively reduce to their identifying glyphs.
13. `+N` reports visual overflow without removing items from `j`/`k`
    navigation, the Inspector, or supported actions.
14. Selecting an omitted item brings it into view and shows its full details
    while `Tab` and `Shift+Tab` continue to switch planner panes.
15. Durations preserve start, continuation, end, and active selection styling
    without adding rows or merging with the current-time marker.

## References

- [SPEC0005: Application View Tabs](SPEC0005-application-view-tabs.md)
- [SPEC0008: Todo Scheduling Metadata](SPEC0008-todo-scheduling-metadata.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [SPEC0013: Operational Console Design System](SPEC0013-operational-console-design-system.md)
