# SPEC 0001: Terminal Splash Screen

## Status

Accepted

## Purpose

Define the first runnable Wolf Todo terminal experience: a branded splash
screen, a placeholder home screen, and configurable command-based exit.

## Configuration

At startup, the application must load `keybindings.toml` from the application
base directory, beside the executable. The file must contain a non-empty quit
command in this schema:

```toml
[keybindings]
quit = ":q"
```

If the file is missing, malformed, or does not contain a non-empty string at
`keybindings.quit`, the application must write a clear error to standard error
and exit with code `1`. It must not render the terminal user interface.

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
transitions to the placeholder home screen. The keypress that dismisses the
splash screen must not be passed to the home screen.

If the terminal is too small for the centered content, render a readable
fallback containing the title and continuation prompt instead of failing.

## Placeholder Home Screen

The placeholder home screen must be vertically and horizontally centered and
display:

- The title `Wolf Todo`.
- The message `Todo manager coming soon`.
- A visible command-input area.

Pressing `:` starts command mode and displays the command line beginning with
`:`. Subsequent typed characters are echoed. Pressing Enter submits the
command, and pressing Esc cancels command mode and clears its input.

The submitted command is compared case-sensitively with
`keybindings.quit`. A matching command exits the application with code `0`.

For an unrecognized command, remain on the home screen and display
`Unknown command: <command>`. Clear this message when the user starts the next
command interaction.

## Acceptance Scenarios

1. With a valid `keybindings.toml`, the application displays the centered wolf
   splash screen and continuation prompt.
2. Pressing any key on the splash screen opens the placeholder home screen.
3. Typing `:q` followed by Enter on the home screen exits with code `0` when
   `quit = ":q"` is configured.
4. Typing an unknown command followed by Enter shows its unknown-command error
   and keeps the home screen open.
5. Pressing Esc while entering a command clears the command without exiting.
6. A missing, malformed, or incomplete `keybindings.toml` writes an error to
   standard error and exits with code `1` before the TUI is rendered.
7. A terminal too small for the centered layout renders the readable fallback
   rather than crashing.

## References

- [ADR0001: Use .NET and Spectre.Console](../adr/ADR0001-use-dotnet-and-spectre-console.md)
- [ADR0002: Use TOML for Command Bindings](../adr/ADR0002-use-toml-for-command-bindings.md)
- [ADR0003: Structure Source Code for Testability](../adr/ADR0003-structure-source-code-for-testability.md)
