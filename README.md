# Overview

This app is a todo manager that saves files in markdown files. The todo's are markdown todo files

## Structure

main
|__ src (source code)
|__ TaskFile.yml (repository automation tasks)
|__ docs (documentation)
    |__ rdr (repository design decisions)
    |__ adr (architectural design decisions)
    |__ plans (store AI plans here)
    |__ spec (contains functional specs)

## Prerequisites

Maintainers need the following tools before working on the project:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for building
  and running the application.
- [PowerShell](https://learn.microsoft.com/powershell/) for repository scripts.
- [Task](https://taskfile.dev/) for running the tasks defined in `TaskFile.yml`.

Verify the tools are available:

```text
dotnet --version
pwsh --version
task --version
```

## Development Workflow

Run repository automation through named tasks declared in `TaskFile.yml`.
This includes build, test, formatting, linting, generation, and maintenance
operations. Do not use repository scripts directly as the normal workflow.

## Running the TUI

Create the global `config.toml` before starting the TUI:

- Linux: `$XDG_CONFIG_HOME/wtodo/config.toml`, or
  `~/.config/wtodo/config.toml` when `XDG_CONFIG_HOME` is unset.
- macOS: `~/Library/Application Support/wtodo/config.toml`.
- Windows: `%APPDATA%\wtodo\config.toml`.

```toml
[projects]
files = [
  "/absolute/path/to/project-one.md",
  "/absolute/path/to/project-two.md"
]

[keybindings]
quit = ":q"
toggle_completed = ":completed"
move_up = ["UpArrow", "k"]
move_down = ["DownArrow", "j"]
focus_next = ["Tab"]
focus_previous = ["Shift+Tab"]
open = ["Enter", "l"]
back = ["Escape", "h"]
command_mode = [":"]
filter_mode = ["/"]
sort_mode = ["t"]
tab_next = ["Ctrl+Tab"]
tab_previous = ["Ctrl+Shift+Tab"]
```

Within `[keybindings]`, only `quit` is required. Omitted bindings use the
defaults shown above. A configured binding array replaces that action's
defaults. Bindings accept printable characters, named console keys, and
`Shift`, `Ctrl`, or `Alt` modifiers such as `Ctrl+K`.

Each configured Markdown file is one project. Start the application with:

```text
task run-tui
```

The TUI remembers the selected project between runs in a separate `state.json`
file under the platform application-state directory. This session state does
not modify project Markdown files or `config.toml`.


## AI Guidance

AGENTS.md files or related files should reference this file for guidence on how to build the application. 
