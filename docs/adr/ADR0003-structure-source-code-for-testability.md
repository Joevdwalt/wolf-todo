# ADR 0003: Structure Source Code for Testability

## Status

Accepted

## Context

Wolf Todo stores todos as Markdown files and will be delivered through more
than one interface. The initial products are the `wtodo-tui` terminal user
interface and the `wtodo` command-line interface. A REPL (`wtodo-repl`) and a
web interface (`wtodo-web`) may follow. Each interface has distinct I/O and
presentation concerns, but must operate on the same todo behavior and Markdown
storage model.

The terminal UI, command-line parsing, filesystem-backed storage, and
file-based configuration introduce I/O concerns that must not obscure
application behavior or make it difficult to verify. The initial project
structure needs to establish clear boundaries before application code is added.

## Decision

Use the following initial source layout:

```text
src/
  WolfTodo.Core/
    WolfTodo.Core.csproj
    Features/
    Infrastructure/
  WolfTodo.Tui/
    WolfTodo.Tui.csproj        # produces wtodo-tui
    Program.cs
    Assets/
      wolf.txt
    Features/
    Infrastructure/
  WolfTodo.Cli/
    WolfTodo.Cli.csproj        # produces wtodo
    Program.cs
    Features/
    Infrastructure/
  WolfTodo.Core.Tests/
    WolfTodo.Core.Tests.csproj
    Features/
    Infrastructure/
  WolfTodo.Tui.Tests/
    WolfTodo.Tui.Tests.csproj
    Features/
    Infrastructure/
  WolfTodo.Cli.Tests/
    WolfTodo.Cli.Tests.csproj
    Features/
    Infrastructure/
  scripts/
```

`WolfTodo.Core` is a class library containing shared todo behavior and any
shared storage abstractions. It must not depend on a presentation host. The
TUI and CLI projects reference `WolfTodo.Core`; they do not reference one
another. Each host owns its interface-specific presentation and adapters. Add
a future host as a new executable project that references `WolfTodo.Core`, for
example `WolfTodo.Repl` producing `wtodo-repl` or `WolfTodo.Web` producing
`wtodo-web`, with a matching test project.

Organize code feature-first within `Features/`. Keep adapters for terminal
interaction, command-line parsing, filesystem access, configuration, and other
external concerns in `Infrastructure/`. Each executable `Program.cs` is that
host's composition root only; it must not contain feature behavior or
infrastructure implementation logic.

Use `Microsoft.Extensions.Hosting` and its built-in dependency-injection
container in each executable host. Register concrete implementations in that
host's composition root. Add interfaces only at side-effect boundaries, where
they allow code to depend on an abstraction for terminal, command-line,
filesystem, configuration, or other I/O.

Design application behavior as pure functions where practical. Isolate I/O
behind injected adapters. Keep methods small and single-purpose. Prefer
immutable data: use records or immutable/read-only collections where suitable,
use `init`-only state where applicable, and do not mutate data owned by a
caller.

Keep classes small and focused on one responsibility. When an orchestration
class begins to grow, split it into focused collaborators rather than extending
a monolith.

Within the TUI host, compose feature screens through a typed application shell.
Keep reusable shell components, such as tab state and presentation, independent
of the feature state hosted beneath them. Fixed application views remain wired
explicitly at the composition root rather than discovered through a plugin
registry.

Do not use discard lambda parameters such as `_ =>`. Use descriptive parameter
names so callbacks remain readable. Language-required discards, such as
`out _`, remain allowed.

Create a corresponding xUnit test project for every executable project, and a
test project for each shared class library. Test projects must use the
`xunit.v3.mtp-v2` package with Microsoft Testing Platform (MTP), the MTP
TRX-report extension, the MTP code-coverage extension, and FluentAssertions.
Do not reference
`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, or Coverlet packages.
For every production class, create one test class named `<ClassName>Tests` in
the mirrored namespace and feature folder. Tests must verify observable
behavior, including each class's failure behavior.

The editable logo source of truth will be
`src/WolfTodo.Tui/Assets/wolf.txt`. The TUI host owns and bundles this asset;
it is not a dependency of the shared core or CLI host. When the TUI executable
project is scaffolded, move the existing logo asset to that location.

## Consequences

### Positive

- Feature behavior can be tested without a terminal, filesystem, or
  configuration file.
- The composition root makes runtime dependencies explicit and keeps feature
  code independent of application startup.
- New interfaces can be added without duplicating todo behavior or coupling
  one presentation host to another.
- A mirrored test structure makes missing tests and their corresponding
  production behavior easy to find.
- Immutable, focused code reduces unintended shared state and limits the scope
  of changes.

### Negative

- Initial project setup includes a shared library, multiple hosts, and
  dependency-injection configuration.
- Adapter boundaries and tests add files and types for behavior that might be
  small at first.
- The one-test-class-per-production-class convention requires ongoing
  maintenance as code evolves.

## References

- [ADR0001: Use .NET and Spectre.Console](ADR0001-use-dotnet-and-spectre-console.md)
- [ADR0002: Use TOML for Command Bindings](ADR0002-use-toml-for-command-bindings.md)
- [RDR0002: Run Repository Scripts Through Tasks](../rdr/RDR0002-run-repository-scripts-through-tasks.md)
- [SPEC0001: Terminal Splash Screen](../spec/SPEC0001-terminal-splash-screen.md)
- [ADR0006: Use a Typed Application Tab Shell](ADR0006-use-a-typed-application-tab-shell.md)
