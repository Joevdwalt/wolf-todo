# SPEC 0008: Todo Scheduling Metadata

## Status

Accepted

## Purpose

Define the Markdown representation used to assign an existing todo to one Day
Planner slot.

## Format

Append one complete ordered pair to the task line:

```markdown
- [ ] Prepare proposal ⏳ 2026-07-15 ⏰ 09:30
```

The date uses `YYYY-MM-DD`. Time uses `HH:mm`, must be `:00` or `:30`, and must
fall between 06:00 and 21:30. Remove the pair from the displayed title and
expose it as structured schedule data. Standalone `⏳` or `⏰` annotations remain
ordinary title text. Duplicate pairs and complete-looking invalid values are
project diagnostics.

Only one todo may occupy a date/time pair across configured projects. The UI
must refuse occupied destinations and expose externally introduced conflicts.
Scheduling, moving, and unscheduling update the original task line using the
conflict-safe strategy in ADR0009.

In the Todos pane, render a scheduled todo's structured value in an adaptive
`SCHEDULED` column as `YYYY-MM-DD HH:mm`; render `-` for unscheduled work. Use
the semantic date color. The shared editor accepts separate ISO date and `HH:mm`
fields. Both blank values unschedule a todo, while partial pairs, off-grid
minutes, and times outside the Planner range are rejected.

Before create or update, reload the catalog and reject a schedule occupied by
another todo across configured projects. Exclude the todo being edited from
that check. Start and due metadata remain parser/serializer compatibility data
and are not part of the interactive scheduling workflow.

## Acceptance Scenarios

1. Valid schedule metadata round-trips through parsing and writing.
2. Invalid dates, off-grid times, and duplicate pairs produce diagnostics.
3. Scheduling preserves surrounding Markdown and external changes are never
   silently overwritten.
4. Existing files without schedule metadata require no migration.
5. Scheduled todos show their date and time in every responsive Todos-pane
   layout without displacing tabs or status controls.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
