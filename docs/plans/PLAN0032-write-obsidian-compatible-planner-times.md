# PLAN 0032: Write Obsidian-Compatible Planner Times

## Goal

Keep Wolf Todo's Planner times without disrupting Obsidian Tasks metadata
recognition.

## Implementation

- Write `⏰ HH:mm` after the task description and before every Tasks marker,
  with `⏳ YYYY-MM-DD` retained as the scheduled-date field in the suffix.
- Parse the time and scheduled date independently so markers may occur between
  them, while continuing to accept the legacy adjacent date-then-clock order.
- Preserve standalone tokens as title text and retain schedule validation and
  duplicate diagnostics.
- Normalize legacy ordering only on the next conflict-safe write to that task;
  do not mutate files during loading or startup.
- Preserve unsupported Tasks metadata such as recurrence, IDs, dependencies,
  and created/completed markers after the clock.

## Verification

- Cover canonical and legacy parsing, intervening metadata, standalone and
  invalid tokens, marker-preserving serialization, and lazy normalization.
- Run `task test` and `git diff --check`.
