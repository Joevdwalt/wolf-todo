# Look up a task by code from the CLI

## Summary

Add `wtodo get <task-code>` for shell users and agents. Resolve the short code
across valid configured Markdown projects and return the exact matching task as
JSON without modifying it.

## Implementation

- Register `get` in CLI dispatch, dependency injection, help text, and known
  command parsing.
- Add a lookup service using the shared `TaskLinkCode` resolver. Include root,
  completed, and nested tasks.
- Extract the list-entry JSON projection into a shared typed output factory so
  `list` and `get` expose identical task fields.
- Return `{ "ok": true, "task": { ... } }`. Do not return ancestors,
  descendants, or a project-filter option.
- Return `invalid_task_code` with exit code `2`; return `task_not_found` or
  `ambiguous_task_code` with exit code `1`.

## Verification

- Cover root, nested, and completed task lookup, including
  `parent_source_line` and output parity with `wtodo list`.
- Cover malformed, missing, and colliding codes, argument errors, help output,
  and unchanged Markdown.
- Run `task build`, `task test`, documentation checks, and `graphify update .`.
