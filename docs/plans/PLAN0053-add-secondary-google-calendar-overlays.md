# PLAN0053: Add Secondary Google Calendar Overlays

## Goal

Show events from configured secondary Google Calendars in the Day Planner
alongside the implicit primary calendar.

## Design

- Add an optional `additional_calendar_ids` array to `[google_calendar]`.
- Keep the agenda provider as a small orchestrator over an injected OAuth event
  source and a focused public event mapper. Load every API page and merge the
  selected day's primary and additional-calendar events while retaining their
  calendar source in event identities.
- Keep successful calendars visible when an additional calendar cannot load,
  and append the affected calendar ID as a warning without replacing normal
  planner status hints.
- Reuse the merged agenda for all-day items, timeline blocks, overlap warnings,
  and the NOW next-meeting countdown.

## Verification

- Give each production class a mirrored focused test class. Cover configuration
  validation, OAuth source creation, pagination, event mapping, partial failure,
  source-safe calendar identities, and merged planner meeting behavior.
- Run `task test` and refresh the Graphify graph.
