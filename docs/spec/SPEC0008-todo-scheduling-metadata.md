# SPEC 0008: Todo Scheduling Metadata

## Status

Accepted

## Purpose

Define the Markdown representation used to schedule an existing todo for a day
or assign it to one Day Planner slot.

## Format

Write Wolf Todo's time extension immediately after the description and before
every Obsidian Tasks marker. Keep the Tasks scheduled date in the metadata
suffix:

```markdown
- [ ] Prepare proposal ⏰ 09:30 ⏱ 30m ⏫ #work ⏳ 2026-07-15
```

The date uses `YYYY-MM-DD`. A valid standalone `⏳ YYYY-MM-DD` is an all-day
schedule. A timed schedule adds `⏰ HH:mm`; time must be `:00`, `:15`, `:30`, or
`:45`, and must fall between 06:00 and 21:45. The tokens need not be adjacent: priority, tags,
recurrence, IDs, dependencies, dates, and other preserved task metadata may
appear between them. Remove both tokens from the displayed title and expose
them as structured schedule data. A time-only `⏰` annotation remains ordinary
title text. Duplicate tokens and complete-looking invalid
values are project diagnostics.

An optional `⏱ <minutes>m` duration is a positive 15-minute multiple from 15m
through 960m. It is an estimate for unscheduled and all-day tasks; for timed
tasks it reserves consecutive Planner slots. A missing duration uses the
configured planner default without modifying the Markdown file.

Read the legacy adjacent `⏳ YYYY-MM-DD ⏰ HH:mm` order for compatibility, but
never write it. A conflict-safe write to a legacy scheduled task normalizes the
clock before all task markers; loading a project performs no migration.

Only one todo may occupy a timed date/time pair across configured projects.
Any number of date-only todos may share a day. The UI must refuse occupied
timed destinations and expose externally introduced timed conflicts.
Scheduling, moving, and unscheduling update the original task line using the
conflict-safe strategy in ADR0009.

In the Todos pane, render a scheduled todo's structured value in an adaptive
`SCHEDULED` column as `YYYY-MM-DD` for all-day or `YYYY-MM-DD HH:mm` for timed
work; render `-` for unscheduled work. Use
the semantic date color. The shared editor accepts separate ISO date and `HH:mm`
fields. The date also accepts `t` for today, `t+N` or `t-N` for days from today,
and `w+N` or `w-N` for weeks from today; a valid expression normalizes to its
ISO date before saving. A date without a time creates an all-day schedule. Both blank values
unschedule a todo, while time without date, off-grid minutes, and times outside
the Planner range are rejected. Planner creation still requires date and time.

Before create or update, reload the catalog and reject a schedule occupied by
another todo across configured projects. Exclude the todo being edited from
that check. Start and due metadata remain parser/serializer compatibility data
and are not part of the interactive scheduling workflow.

## Acceptance Scenarios

1. Clock-first schedule metadata round-trips through parsing and writing while
   Obsidian Tasks continues to recognize the task markers.
2. Invalid dates, time-only values, off-grid times, and duplicate pairs produce diagnostics.
3. Scheduling preserves surrounding Markdown and external changes are never
   silently overwritten.
4. Existing files without schedule metadata require no migration.
5. Scheduled todos show their date and time in every responsive Todos-pane
   layout without displacing tabs or status controls.
6. Legacy date-then-clock schedules load and normalize only when changed.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
