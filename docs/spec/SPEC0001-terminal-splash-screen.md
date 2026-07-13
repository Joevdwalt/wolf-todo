# SPEC 0001: Terminal Splash Screen

## Status

Accepted

## Purpose

Define Wolf Todo's branded splash screen, transition into the functional home
screen, and shared command-mode behavior.

## Configuration

At startup, the application must load the global `config.toml` defined by
ADR0004. The file must contain a non-empty quit command and at least one project
file in this schema:

```toml
[projects]
files = ["/absolute/path/to/project.md"]

[keybindings]
quit = ":q"
```

If the file is missing, malformed, does not contain a non-empty string at
`keybindings.quit`, or does not contain at least one absolute Markdown path at
`projects.files`, the application must write a clear error to standard
error and exit with code `1`. It must not render the terminal user interface.

## Splash Screen

After valid configuration is loaded, clear the terminal and render a vertically
and horizontally centered splash screen containing the ASCII art from
`src/WolfTodo.Tui/Assets/wolf.txt`.

- The title `Wolf Todo`.
- The prompt `Press any key to continue`.

`src/WolfTodo.Tui/Assets/wolf.txt` is the editable, project-owned source of truth
for the logo and may be updated without changing application code. The
application project must bundle the file as an application asset and must not
require a network request or external image viewer to render it. Any keypress
transitions to the project browser defined by SPEC0002. The keypress that
dismisses the splash screen must not be passed to the browser.

If the terminal is too small for the centered content, render a readable
fallback containing the title and continuation prompt instead of failing.

## Command Mode

The project browser must provide a visible command/status area.

Pressing the configured command-mode gesture, `:` by default, starts command
mode and displays the command line beginning with `:`. Subsequent typed
characters are echoed. Pressing Enter submits the command, and pressing Esc
cancels command mode and clears its input.

The submitted command is compared case-sensitively with
`keybindings.quit`. A matching command exits the application with code `0`.

For an unrecognized command, remain in the project browser and display
`Unknown command: <command>`. Clear this message when the user starts the next
command interaction.

## Acceptance Scenarios

1. With a valid global `config.toml`, the application displays the centered wolf
   splash screen and continuation prompt.
2. Pressing any key on the splash screen opens the project browser.
3. Typing `:q` followed by Enter in the browser exits with code `0` when
   `quit = ":q"` is configured.
4. Typing an unknown command followed by Enter shows its unknown-command error
   and keeps the browser open.
5. Pressing Esc while entering a command clears the command without exiting.
6. A missing, malformed, or incomplete global `config.toml` writes an error to
   standard error and exits with code `1` before the TUI is rendered.
7. A terminal too small for the centered layout renders the readable fallback
   rather than crashing.

## References

- [ADR0001: Use .NET and Spectre.Console](../adr/ADR0001-use-dotnet-and-spectre-console.md)
- [ADR0002: Use TOML for Command Bindings](../adr/ADR0002-use-toml-for-command-bindings.md)
- [ADR0003: Structure Source Code for Testability](../adr/ADR0003-structure-source-code-for-testability.md)
- [ADR0004: Use a Global TOML Configuration](../adr/ADR0004-use-a-global-toml-configuration.md)
- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0004: Configurable Browser Key Bindings](SPEC0004-configurable-browser-key-bindings.md)
