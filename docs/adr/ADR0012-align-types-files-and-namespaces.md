# ADR 0012: Align Types, Files, and Namespaces

## Status

Accepted

## Context

Source files are easier to find and navigate when their declared type and
namespace communicate their location consistently. Wolf Todo currently has
types grouped in unrelated files and source layouts that must remain clear as
the application grows.

## Decision

Every named top-level type—class, record, enum, and interface—must be declared
in its own `.cs` file with the same name as the type.

Every source namespace must match the file's folder beneath its project root.
For example, `src/WolfTodo.Tui/Controls/TextBox.cs` declares
`WolfTodo.Tui.Controls`. A `.cs` file directly beneath a project root declares
that project's root namespace. Generated and build-output source is excluded.

Keep implementation-only details private within their owning type when they do
not need a standalone named type. Prefer public accessibility for named control
types so they can be exercised directly by tests; keep private only the methods
and details that do not form a useful control API.

## Consequences

### Positive

- Types are easy to locate by name.
- Namespace imports reflect the source layout.
- Refactors expose unrelated or overly broad source files early.

### Negative

- The project contains more small files.
- Existing grouped types must be split as they are touched.

## References

- [ADR0003: Structure Source Code for Testability](ADR0003-structure-source-code-for-testability.md)
