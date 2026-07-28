# ADR 0013: Use a Markdown Todo-Project Repository

## Status

Accepted

## Context

Markdown files are Wolf Todo's durable store. Reading one file currently
combines document parsing, filesystem access, and catalog loading through a
feature-level parser. Other hosts need to read the same Markdown todo-project
format without inheriting catalog or terminal concerns.

## Decision

Use a read-only `ITodoProjectRepository` as the application boundary for
loading one `TodoProject` from durable storage. Its Markdown implementation,
`MarkdownTodoProjectRepository`, canonicalizes paths, reads through
`IProjectFileSystem`, and returns a `TodoProjectReadResult` rather than
throwing expected missing-file, access, or parse failures.

Keep Markdown syntax in the pure `MarkdownTodoProjectReader`. Its document
`Parse` operation accepts a path and Markdown contents and returns
`ProjectParseResult`; public title, heading, task-line, note-line, and
todo-collection operations, plus the pure `AddNote` todo transformation,
provide reusable semantic parsing units. It performs no filesystem I/O and can
be reused by any host or storage adapter.

`ProjectCatalogLoader` depends on `ITodoProjectRepository` and remains
responsible for configured-file deduplication, catalog sorting, and source-error
presentation. `ProjectTodoMutationService` continues to own the specialised,
conflict-safe Markdown writes from ADR0009. It uses the same reader to
re-validate files before mutation; write repository operations are deliberately
out of scope for this decision.

## Consequences

- Hosts and future adapters can read Markdown todo projects through one public
  data-access boundary.
- Parsing remains deterministic and unit-testable without filesystems.
- Catalog loading no longer knows filesystem details.
- Conflict-safe mutation behaviour remains unchanged, but read and write APIs
  are not yet symmetrical.

## References

- [ADR0003: Structure Source Code for Testability](ADR0003-structure-source-code-for-testability.md)
- [ADR0009: Use Conflict-Safe Markdown Mutations](ADR0009-use-conflict-safe-markdown-mutations.md)
- [ADR0012: Align Types, Files, and Namespaces](ADR0012-align-types-files-and-namespaces.md)
- [SPEC0002: Project Browser and Markdown Todo Format](../spec/SPEC0002-project-browser-and-markdown-todo-format.md)
