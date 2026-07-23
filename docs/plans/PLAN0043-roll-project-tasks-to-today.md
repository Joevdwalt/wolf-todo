# Roll Project Tasks to Today

## Outcome

Provide a fast, conflict-safe way to move incomplete overdue work in one
Markdown project to the current local date.

## Design

- Add `:roll-today` with Tab completion, a Todos command-palette action, and a
  configurable `roll_project_today` binding defaulting to `R`.
- Enable rollover only for a concrete selected project and include incomplete
  top-level and nested todos scheduled before today.
- Preserve scheduled times, durations, task metadata, Markdown formatting, and
  unrelated content while leaving completed, unscheduled, current-day, and
  future tasks unchanged.
- Re-read the project, validate the complete eligible task set, replace all
  affected lines in memory, and perform one atomic write.
- Reload the catalog and restore the selected todo after a successful rollover.

## Verification

- Cover command completion, configuration, reducer and palette availability,
  application routing, atomic Markdown mutation, stale content, selection
  restoration, and no-eligible-task feedback.
- Run `task test` and `git diff --check`.
