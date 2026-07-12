# ADR 0001: Use .NET and Spectre.Console

## Status

Accepted

## Context

Wolf Todo is a todo manager whose todos are stored as Markdown files. The
application needs a maintainable, cross-platform implementation platform and a
rich terminal interface for managing those files interactively.

## Decision

Build Wolf Todo as a .NET 10 console application.

Use the `Spectre.Console` library to provide a full-screen, interactive
terminal user interface. The initial UI is an application shell rather than a
collection of one-off command prompts; detailed screen flows and navigation
will be defined in future specifications or ADRs.

Continue to store todo data in Markdown files. The runtime and UI choices do
not change the persistence model defined in the project README.

## Consequences

### Positive

- .NET 10 provides a current LTS runtime for a new cross-platform application.
- Spectre.Console supplies terminal layout, styling, input, and rendering
  primitives without requiring a graphical UI framework.
- A full-screen terminal UI offers an interactive experience while retaining a
  simple command-line distribution model.
- Markdown files remain portable, inspectable, and version-control friendly.

### Negative

- The application is constrained by terminal capabilities and must handle
  varying terminal sizes and color support.
- Interactive terminal rendering introduces UI state and testability concerns
  beyond a non-interactive command-line tool.
- .NET and Spectre.Console become runtime and package dependencies that must be
  maintained as the application evolves.

## References

- [Project README](../../README.md)
