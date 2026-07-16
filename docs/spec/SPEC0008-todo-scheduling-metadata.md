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

In the Todos pane, render a scheduled todo's structured value on a second line
as `⏳ YYYY-MM-DD HH:mm`, aligned beneath the todo title. Use the semantic date
color with dim emphasis. Treat the title and schedule as one scrolling group so
the schedule is not shown without its todo. If only one content line is
available, show the selected todo title and temporarily omit its schedule.

## Acceptance Scenarios

1. Valid schedule metadata round-trips through parsing and writing.
2. Invalid dates, off-grid times, and duplicate pairs produce diagnostics.
3. Scheduling preserves surrounding Markdown and external changes are never
   silently overwritten.
4. Existing files without schedule metadata require no migration.
5. Scheduled todos show their date and time beneath the title in every
   responsive Todos-pane layout without displacing tabs or status controls.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
