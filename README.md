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
planner_refresh_calendar = ["r"]
create_todo = ["a"]
edit_todo = ["e"]
# Compatibility alias for the same unified editor.
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
background = "#09121B"
surface = "#101C28"
surface_2 = "#162433"
accent = "#F28C28"
accent_bright = "#FFB14A"
info = "#5FA8D3"

[google_calendar]
# Optional: show primary Google Calendar meetings in the Day Planner.
enabled = false
# Required when enabled. Download this desktop OAuth client JSON from Google Cloud.
oauth_client_file = "/absolute/path/to/google-oauth-client.json"
```

Within `[keybindings]`, only `quit` is required. Omitted bindings use the
defaults shown above. A configured binding array replaces that action's
defaults. Bindings accept printable characters, named console keys, and
`Shift`, `Ctrl`, or `Alt` modifiers such as `Ctrl+K`.

The optional `[tui.theme]` table selects the startup theme. Available presets
are `wolf` (the default), `classic`, and `mono`. The configurable semantic
colors are `text`, `accent`, `heading`, `border`, `muted`, `success`,
`warning`, `error`, `tag`, `date`, `background`, `surface`, `surface_2`,
`secondary_text`, `border_active`, `accent_bright`, and `info`. Color values
accept Spectre.Console named colors such as `Cyan`, six-digit hexadecimal
colors such as `#F28C28`, or `default`. Using `default` for a foreground role
uses the terminal foreground; using it for a surface makes that layer
transparent to its enclosing or terminal background. Unknown presets, keys, or
color values are configuration errors.

The optional `[google_calendar]` table adds a read-only primary Google Calendar
overlay to Day Planner. Set `enabled = true` and provide an absolute path to a
Desktop OAuth client JSON file. The first refresh opens Google's consent flow;
the refresh token is stored in Wolf Todo's application-state directory, not in
the project Markdown. `r` refreshes the selected day. Calendar meetings only
warn when a todo shares their time; they never prevent scheduling.

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

The project sidebar includes a virtual `@today` view directly below `All`.
It gathers tasks scheduled for the current local date from every valid project,
keeps project grouping and the active sort, and combines with the `/` filter.
Completed scheduled tasks remain controlled by `:completed`. Because `@today`
is a temporary view, closing there reopens `All` on the next launch.

The interface uses a shared operational-console design across Todos and Day
Planner: a responsive context header, square panels, uppercase structural
labels, adaptive task columns, and configurable semantic foreground and surface
colors. Wide terminals show navigation, tasks, and inspector;
medium terminals prioritize tasks and inspector with navigation available as a
temporary view; narrow terminals show one focused view at a time.

The `Day Planner` tab uses 30-minute slots from 06:00 through 21:30. A todo can
be scheduled for a whole day with `⏳ YYYY-MM-DD`, or assigned to a half-hour
slot with Wolf Todo's `⏰ HH:mm` time before all task markers and the
Obsidian Tasks-compatible `⏳ YYYY-MM-DD` scheduled date, for example
`Prepare proposal ⏰ 09:30 #work ⏳ 2026-07-15`. Enter
assigns an unscheduled todo or moves an existing assignment, `u` unschedules,
and `[`/`]` change days. The planner refuses occupied destination slots.
All-day todos appear above the timeline. When Google Calendar is configured,
all-day events and focus/status entries share that header, while timed meetings
appear in their slots and warn on overlaps. Scheduled todos show either
`YYYY-MM-DD` or `YYYY-MM-DD HH:mm` in the adaptive `SCHEDULED` column in
the Todos pane. The shared field editor can schedule or unschedule work, and
`d`/`D` sort by scheduled date and time. Existing start and due annotations are
preserved in Markdown but intentionally omitted from the normal UI. The planner
shows responsive details for the selected slot; `v` hides or
restores them. On today, a bright, full-width `▶────` timeline row shows the exact current
time and refreshes once per idle minute without borrowing the panel-border
style. Its unscheduled-todo picker shows several filterable candidates.
On an occupied slot, `e` or `E`, Ctrl+E, and Space provide the same task editing,
external editing, and completion actions as the Todos tab. Creating with `a`
uses the complete task editor, pre-fills the selected slot, and
requires a schedule. Rescheduling from the Planner editor follows the task to
its new date and slot.

In the Todos tab, `a` creates a todo under the chosen project's `## Inbox`.
`e` opens one task editor for title, reference, priority, tags, schedule, notes,
and direct subtasks; `E` is a compatibility alias for the same editor. It uses
one cursor across compact field rows and a source-ordered content outline.
Notes use `•`; open and completed subtasks use `◯` and `✓`. Use `a` to choose
and insert content after the selected item (or append when a field is selected),
`e` or open to edit, `d` to remove, Space to toggle a subtask, and Ctrl+S to save
the entire task in one conflict-safe Markdown write. Removing a subtask with
descendants requires confirmation. Space outside the editor changes the
selected task's Markdown checkbox.

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
Todos with tags show a compact `#work #now` line beneath the title. The tag
line follows the task's tree indentation and remains attached to its task while
the list scrolls. Tree continuation bars remain visible through tag lines so
sibling relationships are not interrupted.


## AI Guidance

AGENTS.md files or related files should reference this file for guidence on how to build the application. 
