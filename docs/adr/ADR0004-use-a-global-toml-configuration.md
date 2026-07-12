# ADR 0004: Use a Global TOML Configuration

## Status

Accepted

## Context

Wolf Todo has multiple executable hosts that must share the same project
catalog and user preferences. The earlier executable-adjacent keybinding file
defined by ADR0002 would duplicate configuration between the TUI, CLI, and any
future REPL or web host.

Project files can reside in one or more directories outside the application
installation. Configuration therefore belongs to the user rather than to an
individual executable.

## Decision

Store user configuration in one global `config.toml` file. Use these
platform-standard locations:

- Linux: `$XDG_CONFIG_HOME/wtodo/config.toml`, falling back to
  `~/.config/wtodo/config.toml` when `XDG_CONFIG_HOME` is unset or empty.
- macOS: `~/Library/Application Support/wtodo/config.toml`.
- Windows: `%APPDATA%\wtodo\config.toml`.

The initial schema is:

```toml
[projects]
directories = [
  "/absolute/path/to/projects",
  "/another/project/directory"
]

[keybindings]
quit = ":q"
```

`projects.directories` must contain at least one absolute directory path.
Discover immediate `.md` files in each directory; do not search recursively.
Canonicalize and deduplicate file paths before loading projects.

Use the `Tomlyn` NuGet package to parse the configuration. A missing,
malformed, or incomplete global configuration is a startup failure. Write a
clear error to standard error and exit with code `1` before rendering an
interactive interface.

A valid configuration that names a missing or unreadable directory is not a
startup failure. Hosts must preserve that source as a catalog error so users
can diagnose it while continuing to access valid projects.

This decision supersedes ADR0002. TOML and the configurable quit command are
retained, but the executable-adjacent `keybindings.toml` file is replaced by
the shared global configuration.

## Consequences

### Positive

- All current and future hosts share one project catalog and user preferences.
- Configuration survives application upgrades and does not depend on the
  launch directory.
- Adding a Markdown file to a configured directory automatically creates a
  project in Wolf Todo.
- Platform-standard locations integrate with normal user configuration backup
  and management practices.

### Negative

- Configuration discovery differs by operating system.
- Users must provide absolute paths, reducing portability between machines.
- A missing or malformed global configuration prevents application startup.
- Directory and file errors require nonfatal diagnostics in each host.

## References

- [ADR0001: Use .NET and Spectre.Console](ADR0001-use-dotnet-and-spectre-console.md)
- [ADR0002: Use TOML for Command Bindings](ADR0002-use-toml-for-command-bindings.md)
- [ADR0003: Structure Source Code for Testability](ADR0003-structure-source-code-for-testability.md)
- [SPEC0001: Terminal Splash Screen](../spec/SPEC0001-terminal-splash-screen.md)
- [SPEC0002: Project Browser and Markdown Todo Format](../spec/SPEC0002-project-browser-and-markdown-todo-format.md)
