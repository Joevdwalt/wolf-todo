# RDR 0004: Place global.json at the Repository Root

## Status

Accepted

## Context

Wolf Todo uses `global.json` to configure .NET 10's Microsoft Testing Platform
runner for `dotnet test`. The .NET CLI searches for `global.json` from its
current working directory and continues through ancestor directories. Repository
tasks and CI run .NET commands from the repository root, while application
projects reside beneath `src/`.

Placing the file in `src/` would not apply its settings to a `dotnet` command
started at the repository root, because the CLI does not search into child
directories.

## Decision

Keep the repository-wide `global.json` file at the repository root.

It contains settings that must apply to all projects and repository automation,
including the Microsoft Testing Platform setting required by RDR0003. Run
repository `dotnet` commands from the repository root through their declared
tasks.

Do not place duplicate `global.json` files beneath `src/` or individual
projects. A nested `global.json` is permitted only when a future, explicitly
scoped exception requires different .NET CLI behavior; document that exception
in a subsequent RDR.

## Consequences

### Positive

- Local development, task automation, and CI resolve the same .NET CLI
  configuration.
- Current and future projects inherit the testing-runner setting without
  duplicated configuration.
- Repository-wide tooling remains discoverable at the repository root.

### Negative

- Direct commands run outside the repository root and its descendants do not
  use this configuration.
- A project needing different CLI behavior requires an explicit documented
  exception.

## References

- [RDR0001: Document Naming Convention](RDR0001-document-naming-convention.md)
- [RDR0002: Run Repository Scripts Through Tasks](RDR0002-run-repository-scripts-through-tasks.md)
- [RDR0003: Build Application Projects and Run Unit Tests Through Tasks](RDR0003-build-application-projects-and-run-unit-tests-through-tasks.md)
- [global.json overview (.NET)](https://learn.microsoft.com/dotnet/core/tools/global-json)
