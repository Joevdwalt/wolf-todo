# SPEC 0019: Agent Task Import CLI

## Status

Accepted

## Purpose

Allow AI agents and shell users to create tasks in the same explicitly
configured Markdown projects loaded by `wtodo-tui`.

## Commands

`wtodo add` creates one task. It requires `--project` and `--title` and accepts
`--reference`, `--priority`, repeated `--tag`, `--scheduled`, `--time`,
`--duration-minutes`, and ordered repeated `--note`, `--subtask`, and
`--completed-subtask` options.

`wtodo import` accepts exactly one of `--file <path>` or `--stdin`. Its UTF-8
JSON document contains one non-empty `project` and one non-empty `tasks` array.
Each task may contain:

```json
{
  "title": "Prepare proposal",
  "reference": "EXT-42",
  "priority": "high",
  "tags": ["now"],
  "schedule": { "date": "2026-09-01", "time": "09:30" },
  "duration_minutes": 30,
  "content": [
    { "type": "note", "text": "Review scope" },
    { "type": "subtask", "title": "Draft", "completed": false }
  ]
}
```

JSON uses the exact snake-case names above and rejects unknown properties.
Priority values are `lowest`, `low`, `medium`, `high`, and `highest`.
Schedules use ISO dates and optional `HH:mm` times; relative TUI date
expressions are not accepted. Content order is preserved and subtasks are
direct children only.

`wtodo list` returns every task from configured projects. An optional
`--project <title|absolute-path>` limits the result to one resolved configured
project, using the same title and path resolution rules as task creation. The
result includes each task's project, Markdown source line, parent source line
for nested tasks, completion state, metadata, schedule, duration, and notes.

## Project and Mutation Behavior

Resolve an absolute target only when its canonical path is in
`projects.files`. Resolve any other target as a unique case-insensitive project
display title. Reject unconfigured paths, missing or invalid projects, and
ambiguous titles.

One invocation targets one project. Validate every task and timed schedule
against a freshly loaded configured catalog before writing. A timed schedule
must satisfy SPEC0008 and must not collide with an existing configured task or
another task in the batch. Append tasks in input order beneath `## Inbox` using
one conflict-safe atomic replacement. On any failure, create no tasks. Exact
duplicate task content remains valid.

The CLI does not notify a running TUI. Changes appear on the next catalog
reload or launch.

## Results

Command execution writes exactly one JSON object to standard output. Creation success
contains `ok: true`, resolved project title/path, `created_count`, and ordered
zero-based input indexes with one-based Markdown `source_line` values. Failure
contains `ok: false` and an error `code` and `message`.

List success contains `ok: true`, `task_count`, and source-ordered `tasks`.

Use exit code `0` for success and help, `2` for command, option, JSON-schema, or
task-field errors, and `1` for configuration, project resolution, schedule
conflict, parse, or write failures.

## References

- [ADR0004: Use a Global TOML Configuration](../adr/ADR0004-use-a-global-toml-configuration.md)
- [ADR0009: Use Conflict-Safe Markdown Mutations](../adr/ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0008: Todo Scheduling Metadata](SPEC0008-todo-scheduling-metadata.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
