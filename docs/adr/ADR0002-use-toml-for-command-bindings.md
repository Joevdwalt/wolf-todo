# ADR 0002: Use TOML for Command Bindings

## Status

Accepted

## Context

Wolf Todo needs file-driven command bindings so maintainers and users can
change supported commands without modifying application code. The initial
terminal experience requires a configurable quit command, `:q` by default.

The application is a .NET console application. JSON is .NET's native
configuration convention, but a dedicated keybinding file benefits from a
compact, human-editable format that can evolve as bindings grow.

## Decision

Store command bindings in a required `keybindings.toml` file beside the
application executable.

Use the `Tomlyn` NuGet package to parse the TOML file. The initial schema is:

```toml
[keybindings]
quit = ":q"
```

The application must validate the file at startup. A missing, malformed, or
incomplete file is a startup failure: write a clear error to standard error and
exit with code `1` before rendering the terminal user interface.

Detailed command-input and screen behavior is defined by
`SPEC0001-terminal-splash-screen.md`.

## Consequences

### Positive

- Command bindings can change without recompiling the application.
- TOML is compact and readable for a small, hand-maintained bindings file.
- A dedicated file keeps user interaction bindings separate from general
  application settings.
- Strict startup validation prevents the application from running with an
  unknown command configuration.

### Negative

- Tomlyn adds a third-party dependency because .NET does not include TOML
  parsing.
- The file must be deployed beside the executable.
- A configuration error prevents the application from starting until corrected.

## References

- [SPEC0001: Terminal Splash Screen](../spec/SPEC0001-terminal-splash-screen.md)
- [ADR0001: Use .NET and Spectre.Console](ADR0001-use-dotnet-and-spectre-console.md)
