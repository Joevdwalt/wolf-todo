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
help = ":help"
move_up = ["UpArrow", "k"]
move_down = ["DownArrow", "j"]
jump_top = ["g"]
jump_bottom = ["G"]
focus_next = ["Tab"]
focus_previous = ["Shift+Tab"]
open = ["Enter", "l"]
back = ["Escape", "h"]
command_mode = [":"]
command_palette = ["?"]
filter_mode = ["/"]
sort_mode = ["t"]
tab_next = ["L"]
tab_previous = ["H"]
planner_previous_day = ["["]
planner_next_day = ["]"]
planner_today = ["g"]
planner_unschedule = ["u"]
create_todo = ["a"]
edit_todo = ["e"]
edit_todo_content = ["E"]
toggle_todo = ["Spacebar"]
toggle_details = ["v"]
remove_content = ["d"]
save_form = ["Ctrl+S"]

[tui.theme]
preset = "wolf"
# Any preset color can be overridden with a Spectre.Console color name,
# a #RRGGBB value, or "default".
accent = "#5FD7FF"
heading = "#AF87FF"
```

Within `[keybindings]`, only `quit` is required. Omitted bindings use the
defaults shown above. A configured binding array replaces that action's
defaults. Bindings accept printable characters, named console keys, and
`Shift`, `Ctrl`, or `Alt` modifiers such as `Ctrl+K`.

The optional `[tui.theme]` table selects the startup theme. Available presets
are `wolf` (the default), `classic`, and `mono`. The configurable semantic
colors are `text`, `accent`, `heading`, `border`, `muted`, `success`,
`warning`, `error`, `tag`, and `date`. Color values accept Spectre.Console
named colors such as `Cyan`, six-digit hexadecimal colors such as `#5FD7FF`,
or `default` to use the terminal foreground. Unknown presets, keys, or color
values are configuration errors.

Each configured Markdown file is one project. Start the application with:

```text
task run-tui
```

The TUI remembers the selected project and todo sort between runs in a separate
`state.json` file under the platform application-state directory. Every launch
still opens the Todos tab with keyboard focus in its todo list. This session
state does not modify project Markdown files or `config.toml`.

The `Day Planner` tab uses 30-minute slots from 06:00 through 21:30. Scheduling
a todo adds `⏳ YYYY-MM-DD ⏰ HH:mm` to its original Markdown task line. Enter
assigns an unscheduled todo or moves an existing assignment, `u` unschedules,
and `[`/`]` change days. The planner refuses occupied destination slots.

In the Todos tab, `a` creates a todo under the chosen project's `## Inbox`
heading, `e` edits the selected todo's parsed fields, and Space changes its
Markdown checkbox. Ctrl+S saves the create/edit form. Writes re-read and
validate the source before atomically replacing it so external changes are not
silently overwritten.

Uppercase `E` opens the structured notes and subtasks editor. It shows the
selected todo's direct content; opening it on a subtask supports deeper nesting.
Use `a` to add, `e` to edit, `d` to remove, Space to toggle a subtask, and
Ctrl+S to atomically save the draft. Removing a subtask with descendants
requires confirmation.

Command mode belongs to the application shell: `:q`, `:completed`, and unknown
command feedback work from either Todos or Day Planner. An active feature
picker, filter, move, or edit form receives input before global commands.
`?` or `:help` opens the global searchable command palette. Disabled actions
remain visible with a reason; `/` searches and Enter runs the selected action.
In the Todos tab, `v` hides or restores the detail preview for the current
session. Opening a todo restores hidden details automatically.
The Vim-style `g` and `G` bindings jump to the first or last item in the
focused Projects or Todos list. Planner keeps its contextual `g` binding for
returning to today.


## AI Guidance

AGENTS.md files or related files should reference this file for guidence on how to build the application. 
