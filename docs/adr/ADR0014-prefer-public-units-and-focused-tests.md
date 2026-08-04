# ADR 0014: Prefer Public Units and Focused Tests

## Status

Accepted

## Context

Wolf Todo's hosts contain composition and rendering code that can easily grow
into broad orchestration classes. When behavior is hidden behind private
methods, tests are forced to exercise only the top-level API. Those tests are
valuable for integration coverage, but they become expensive to understand and
maintain when they are the primary way to verify small decisions.

The public host API, such as `ITerminalUi`, exists to glue the program
together. It should not be the only practical test surface for rendering,
measurement, formatting, reducers, presenters, or other application behavior.

## Decision

Methods should be public by default when they represent a useful unit of
behavior that can be named, understood, and tested directly.

Use private methods only for incidental implementation details that are not a
stable or useful behavior boundary. Do not keep meaningful behavior private
solely because it is currently called from one class.

Prefer extracting focused public classes or public methods over growing a large
adapter with many private helpers. Composition roots and adapter APIs should
remain small and primarily glue focused units together.

Tests should target the smallest practical observable behavior. Maintain a
small set of top-level API tests to prove wiring and integration, but do not
rely on comprehensive tests through the main API when behavior can be verified
through focused units.

For each production class, keep the mirrored test class convention from
ADR0003. Within that test class, prefer direct tests of public behavior over
testing the same logic indirectly through a larger adapter.

## Consequences

### Positive

- Important behavior has a direct, stable test surface.
- Tests become smaller, clearer, and easier to diagnose.
- Adapter classes stay focused on orchestration instead of hiding feature logic.
- Refactoring pressure appears earlier when a method wants to stay private only
  because no focused owner exists.

### Negative

- Public APIs inside a project become broader.
- Some implementation details must be named carefully so the public surface
  communicates intent instead of exposing accidental mechanics.
- More focused tests may replace fewer broad tests, which requires discipline to
  keep integration coverage intentionally small but still present.

## References

- [ADR0003: Structure Source Code for Testability](ADR0003-structure-source-code-for-testability.md)
- [ADR0012: Align Types, Files, and Namespaces](ADR0012-align-types-files-and-namespaces.md)
