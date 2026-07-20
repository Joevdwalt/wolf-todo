# PLAN0036: Google Calendar Planner Overlay

## Goal

Make the Day Planner aware of meetings while keeping Markdown todos as the only
writeable task source.

## Design

- Model a schedule as a required date with an optional time. A date-only
  `⏳ YYYY-MM-DD` marker is an all-day todo; timed slots retain the existing
  clock-first `⏰ HH:mm ... ⏳ YYYY-MM-DD` representation.
- Add a small calendar-agenda provider boundary and an in-memory day cache so
  the TUI remains responsive while Google OAuth or network calls run.
- Configure the feature through an optional `[google_calendar]` TOML table with
  `enabled` and an absolute `oauth_client_file`. Use Google desktop OAuth for
  read-only primary-calendar access and persist its refresh token only under
  Wolf Todo application state.
- Render all-day todos and Google all-day/status events in a one-line strip
  above the planner timeline. Render timed meetings in their overlapping slots.
  A todo that overlaps a meeting shows a warning but remains assignable.
- Bind `r` to refresh the selected day and expose the same action in the global
  command palette. Surface syncing, authentication, configuration, and offline
  states as non-blocking planner status.

## Verification

- Cover date-only parser, mutation, editor, planner, and configuration paths.
- Cover planner agenda rendering and meeting-overlap presentation.
- Run `task test`.
