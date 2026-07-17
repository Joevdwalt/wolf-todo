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
edit_todo_external = ["Ctrl+E"]
toggle_todo = ["Spacebar"]
toggle_details = ["v"]
remove_content = ["d"]
save_form = ["Ctrl+S"]

[tui.theme]
preset = "wolf"
# Any preset color can be overridden with a Spectre.Console color name,
# a #RRGGBB value, or "default".
accent = "#5FD7FF"
heading = "#FFAF5F"
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

To publish the TUI and make `wtodo-tui` available from your shell, run:

```text
task install-tui
```

On macOS and Linux this creates `~/.local/bin/wtodo-tui`, linked to a
framework-dependent Release publish in the platform user-data directory. On
Windows it creates `%USERPROFILE%\bin\wtodo-tui.cmd`. The task warns when the
launcher directory is not already on `PATH`. Set `WTODO_INSTALL_DIR` or
`WTODO_LINK_DIR` to override either location before running the task. Re-run
the task after updating Wolf Todo to replace the published application.

The TUI remembers the selected project and todo sort between runs in a separate
`state.json` file under the platform application-state directory. Every launch
still opens the Todos tab with keyboard focus in its todo list. This session
state does not modify project Markdown files or `config.toml`.

The interface uses a shared operational-console design across Todos and Day
Planner: a responsive context header, square panels, uppercase structural
labels, adaptive task columns, and semantic foreground colors while preserving
the terminal background. Wide terminals show navigation, tasks, and inspector;
medium terminals prioritize tasks and inspector with navigation available as a
temporary view; narrow terminals show one focused view at a time.

The `Day Planner` tab uses 30-minute slots from 06:00 through 21:30. Scheduling
a todo adds `⏳ YYYY-MM-DD ⏰ HH:mm` to its original Markdown task line. Enter
assigns an unscheduled todo or moves an existing assignment, `u` unschedules,
and `[`/`]` change days. The planner refuses occupied destination slots.
Scheduled todos show `YYYY-MM-DD HH:mm` in the adaptive `SCHEDULED` column in
the Todos pane. The shared field editor can schedule or unschedule work, and
`d`/`D` sort by scheduled date and time. Existing start and due annotations are
preserved in Markdown but intentionally omitted from the normal UI. The planner
shows responsive details for the selected slot; `v` hides or
restores them. Its unscheduled-todo picker shows several filterable candidates.
On an occupied slot, `e`, `E`, Ctrl+E, and Space provide the same field editing,
content editing, external editing, and completion actions as the Todos tab.
Creating with `a` uses the full todo form, pre-fills the selected slot, and
requires a schedule. Rescheduling from the Planner editor follows the task to
its new date and slot.

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

Ctrl+E opens the selected todo's Markdown project at its source line in the
terminal editor named by `$EDITOR`. Wolf Todo waits for the editor, then reloads
the project. Helix, Vim-family editors, and Nano receive their supported line
argument; other editors open the file without a line position. `$EDITOR` must
contain an executable name or path without additional arguments.

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

Nested todos are always expanded in the Todos list and inspector. Unicode
`├─`, `└─`, and `│` connectors show sibling and ancestor relationships. A
filter that matches a descendant keeps its visible ancestor path as normal,
selectable todo rows so the result retains useful tree context.


## AI Guidance

AGENTS.md files or related files should reference this file for guidence on how to build the application. 
