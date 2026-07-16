# ADR 0010: Edit Markdown with the Configured External Editor

## Status

Accepted

## Context

Structured TUI forms intentionally edit only supported todo fields and direct
content. Users also need the full capabilities of their terminal editor while
keeping Markdown files as the application's durable store.

## Decision

Open the selected todo's canonical project file directly in the executable
named by `$EDITOR`, positioned at its one-based source line when the editor has
known syntax. Support Helix `path:line`, Vi-family and Nano `+line path`, and
fall back to the plain path for unknown editors. Treat `$EDITOR` as one
executable name or path and pass every argument without a shell.

Suspend TUI rendering and show the cursor while the attached editor process is
running. Wait for it to exit, reset the terminal, and reload the configured
catalog whenever a process started. Preserve browser context by list position
rather than reusing a source-line identity that the external edit may have
invalidated. Report configuration, launch, and exit failures in the browser.

External edits are owned by the editor and do not use Wolf Todo's mutation
service. Conflict-safe validation in ADR0009 continues to apply to mutations
performed by Wolf Todo itself.

## Consequences

- Users can edit arbitrary Markdown without expanding the structured form.
- File paths containing spaces are passed safely through argument lists.
- Unknown editors open the correct file but may not jump to the todo.
- Editor commands containing embedded arguments require a future command model.
- Malformed external changes appear through normal project diagnostics.

## References

- [ADR0009: Use Conflict-Safe Markdown Mutations](ADR0009-use-conflict-safe-markdown-mutations.md)
- [SPEC0010: Writable Todo Workflows](../spec/SPEC0010-writable-todo-workflows.md)
