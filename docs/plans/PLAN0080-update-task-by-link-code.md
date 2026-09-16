# PLAN 0080: Update a Task by Link Code

## Summary

Add a patch-style `wtodo update <task-code>` command for changing configured
Markdown tasks by their `wt1-` location code. Preserve omitted fields and use
the existing conflict-safe atomic mutation path.

## Implementation

- Register an `UpdateCommand` and handler with structured parsing, help text,
  dependency injection, and the existing CLI error conventions.
- Support completion, title, reference, priority, tags, schedule, duration,
  and content updates. Use explicit `--clear-*` options for removals; preserve
  direct subtasks and all omitted fields.
- Add a focused update service that resolves root, completed, and nested tasks,
  validates effective schedules while excluding the target, merges the patch
  with the current snapshot, and returns the updated list-entry shape.
- Extend the Core task update model so completion and field/content changes are
  written together through one conflict-safe atomic replacement.
- Document the command and result/error contract in `README.md`,
  `docs/spec/SPEC0019-agent-task-import-cli.md`, and
  `docs/spec/SPEC0023-task-links.md`.

## Verification

- Cover root and nested completion changes, multi-field patches, explicit
  clearing, preserved omitted fields/subtasks, output parity, schedule checks,
  stale snapshots, and atomic failure behavior.
- Cover malformed, missing, and ambiguous codes; missing updates; duplicate or
  conflicting options; invalid values; and help output.
- Run `task build`, `task test`, `git diff --check`, and `graphify update .`.
