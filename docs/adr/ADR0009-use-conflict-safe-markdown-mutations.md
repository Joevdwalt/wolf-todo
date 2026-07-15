# ADR 0009: Use Conflict-Safe Markdown Mutations

## Status

Accepted

## Context

Scheduling and editing require Wolf Todo to update project Markdown without
silently overwriting changes made by an editor or damaging surrounding notes,
subtasks, and formatting.

## Decision

Before each mutation, re-read and re-parse the project, then verify the target
path, source line, and parsed todo snapshot. Refuse stale or ambiguous targets.
Change only the target task line, preserve surrounding content and newline
conventions, and replace the file atomically through a temporary sibling file.
Preserve Unix file permissions during replacement.

Notes carry source-line identities. Structured content saves validate the
complete selected subtree, then apply note and direct-subtask edits as one
bottom-up line mutation. Unchanged descendant lines retain their original
formatting; removing a subtask removes its identified descendant subtree.

New todos are appended beneath a case-insensitive `## Inbox` heading. Create
the heading when absent and reject files containing multiple matching Inbox
headings.

## Consequences

- External changes fail safely and remain available for reload.
- Markdown remains the only durable todo and planner store.
- Source-line identities can become stale and must be revalidated for every
  write.
- Task-line metadata is normalized when that todo is edited.
- Note identities allow precise edits without normalizing unrelated content.

## References

- [ADR0004: Use a Global TOML Configuration](ADR0004-use-a-global-toml-configuration.md)
- [SPEC0008: Todo Scheduling Metadata](../spec/SPEC0008-todo-scheduling-metadata.md)
- [SPEC0010: Writable Todo Workflows](../spec/SPEC0010-writable-todo-workflows.md)
