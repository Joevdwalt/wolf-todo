# SPEC 0016: Weekly Task Time Tracking

## Status

Accepted

## Behavior

Configure timing with `[timer].notes_directory`, an absolute directory. The
configurable `toggle_timer` binding (default `Ctrl+T`) starts or stops the
selected todo in Todos or Day Planner. Only one timer may run. Toggling another
todo records the active session, then starts the new one. Calendar-only items
and conflicting Planner slots cannot be timed.

While active, both views show a dedicated, pulsing
`TIMER HH:mm · <todo title>` status row and refresh once per second. A normal
application exit records the active timer; failed writes leave it active and
show an error.

Write sessions atomically to `YYYY/MM/Time - <ISO-week>.md`. A file has a
weekly title and dated headings; entries use `HH:mm–HH:mm · Nm — Project · Todo`.
Split sessions at local midnight so each portion belongs to the appropriate ISO
week file. Durations are whole minutes rounded up to at least one minute.

## Acceptance Scenarios

1. A selected todo starts and stops with Ctrl+T, producing one weekly Markdown entry.
2. Switching selected todos records the first session before timing the second.
3. An active timer is visible in both Todos and Day Planner and advances while idle.
4. A session crossing midnight writes each portion under the correct date/week.
5. Missing timer configuration disables timing without blocking normal todo work.
