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

### Testing in Rider

Wolf Todo uses Microsoft Testing Platform (MTP) with xUnit v3. To discover and
run tests in Rider:

1. Open `WolfTodo.sln` and build the solution at least once.
2. Open **Settings** → **Build, Execution, Deployment** → **Unit Testing** →
   **Testing Platform**.
3. Enable Microsoft Testing Platform test discovery.

Rider discovers MTP tests after their test project has been built. The tests
then appear in the Unit Tests window and as run/debug gutters in test files.

## Running the TUI

## Component sandboxes

Reusable terminal controls can be run without loading project Markdown or
starting the full application. The task edit dialog has a rich interactive
fixture:

```text
task component:dialog
task component:textbox
task component:textbox:edit
task component:textbox:readonly
task component:multiline
task component:select-list
```

The sandbox uses the same dialog renderer and editor reducer as the TUI, but
captures save actions in memory and never writes files. To debug it in an IDE,
set `WolfTodo.ComponentHarness` as the startup project and use its `Task edit
dialog`, `Text box edit`, `Text box readonly`, `Multiline text box`, or
`Select list` launch profile.

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

[[sidebar.items]]
title = "@yesterday"
query = "scheduled:t-1"
order = "scheduled asc"

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
planner_toggle_view = ["s"]
planner_increase_range = ["+"]
planner_decrease_range = ["-"]
planner_previous_column = ["h"]
planner_next_column = ["l"]
planner_today = ["T"]
planner_unschedule = ["u"]
planner_refresh_calendar = ["r"]
planner_export_schedule = ["x"]
toggle_timer = ["Ctrl+T"]
start_pomodoro = ["Ctrl+P"]
start_untracked_pomodoro = ["Ctrl+Shift+P"]
create_todo = ["a"]
edit_todo = ["e"]
# Compatibility alias for the same unified editor.
edit_todo_content = ["E"]
edit_todo_external = ["Ctrl+E"]
toggle_todo = ["Spacebar"]
toggle_todo_selection = ["m"]
bulk_edit_todos = ["b"]
clear_todo_selection = ["Ctrl+M"]
toggle_details = ["v"]
roll_project_today = ["R"]
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
now = "#FF5CA8"
timer = "#C6F36A"

[google_calendar]
# Optional: show primary and additional Google Calendar meetings in the Day Planner.
enabled = false
# Required when enabled. Download this desktop OAuth client JSON from Google Cloud.
oauth_client_file = "/absolute/path/to/google-oauth-client.json"
# Calendar IDs to overlay in addition to the implicit primary calendar.
additional_calendar_ids = [
  "team@example.com",
  "abc123@group.calendar.google.com"
]

[planner]
# New tasks created from Day Planner receive this explicit ⏱ duration.
default_duration_minutes = 30

[planner.export]
# Optional: export the selected Day Planner date to this weekly-notes tree.
notes_directory = "/absolute/path/to/general-day-notes"
# Raw Markdown/Obsidian links emitted below every exported date heading.
project_links = [
  "[[kohde/2026/Todos - Kohde]]",
  "[[pers/2026/Todo - Personal]]"
]

[timer]
notes_directory = "/absolute/path/to/time-logs"
# Focus countdown length, from 1 through 180 minutes.
pomodoro_minutes = 25
# Play an audible completion alert when a Pomodoro finishes.
# Wolf Todo uses a native system sound and falls back to the terminal bell.
bell = true
```

Within `[keybindings]`, only `quit` is required. Omitted bindings use the
defaults shown above. A configured binding array replaces that action's
defaults. Bindings accept printable characters, named console keys, and
`Shift`, `Ctrl`, or `Alt` modifiers such as `Ctrl+K`.

In Day Planner, `s` switches between a day and a two-to-three-day view. In the
multiday view, `h` and `l` move the active date between columns (scrolling the
range when needed), while `-` and `+` shrink or grow the visible range.

The optional `[tui.theme]` table selects the startup theme. Available presets
are `wolf` (the default), `classic`, and `mono`. The configurable semantic
colors are `text`, `accent`, `heading`, `border`, `muted`, `success`,
`warning`, `error`, `tag`, `date`, `background`, `surface`, `surface_2`,
`secondary_text`, `border_active`, `accent_bright`, `info`, `now`, and `timer`. Color values
accept Spectre.Console named colors such as `Cyan`, six-digit hexadecimal
colors such as `#F28C28`, or `default`. Using `default` for a foreground role
uses the terminal foreground; using it for a surface makes that layer
transparent to its enclosing or terminal background. Unknown presets, keys, or
color values are configuration errors.

Enter `:dump-screen` to save the current rendered frame as a plain-text file
under `screen-dumps/` in the directory where Wolf Todo was launched. The app
shows the exact saved path in its status message.

The optional `[google_calendar]` table adds a read-only Google Calendar overlay
to Day Planner. It always loads the primary calendar, plus any IDs in
`additional_calendar_ids` (find an ID in the calendar's Google Calendar
integration settings). Set `enabled = true` and provide an absolute path to a
Desktop OAuth client JSON file. The first refresh opens Google's consent flow;
the refresh token is stored in Wolf Todo's application-state directory, not in
the project Markdown. `r` refreshes the selected day. Calendar meetings only
warn when a todo shares their time; they never prevent scheduling. If an
additional calendar is unavailable, successfully loaded calendars stay visible
and the planner identifies the failed calendar in its status line.

The optional `[timer]` table enables one active task timer. `Ctrl+T` starts or
stops the selected todo (or switches to another selected todo), shows elapsed
time in a dedicated pulsing status row in both Todos and Day Planner, and
writes sessions to `YYYY/MM/Time - <ISO-week>.md` below `notes_directory`.
`Ctrl+P` opens a Pomodoro-minutes prompt for the selected todo, or an untracked
Pomodoro when no todo is selected. A selected todo's explicit `⏱` duration is
the prompt default; otherwise it uses `pomodoro_minutes` (25 by default).
`Ctrl+Shift+P` opens the same prompt for an untracked Pomodoro. Use
`:pomodoro 45` for an immediate one-off countdown, `:pomodoro task` to use the
selected todo's `⏱` duration, or `:pomodoro 10 --untracked` to start without a
todo. One-off values may be from 1 through 960 minutes and do not change the
configured default. Completion shows a persistent in-app banner until your
next keypress and requests a desktop notification. When `bell` is enabled,
Wolf Todo plays a native completion sound (falling back to the terminal bell if
needed). Task-linked Pomodoros are written to the weekly log; untracked
Pomodoros are not. Wolf Todo records an active task-linked timer when it exits
normally.

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

## Loading tasks from the CLI

The `wtodo` CLI writes tasks into the same configured Markdown projects loaded
by `wtodo-tui`. Install it with:

```text
task install-cli
```

This creates `~/.local/bin/wtodo` on macOS and Linux or
`%USERPROFILE%\bin\wtodo.cmd` on Windows. Set `WTODO_CLI_INSTALL_DIR` or
`WTODO_LINK_DIR` to override the publish or launcher directory.

Create one task by configured project title or absolute configured path:

```text
wtodo add --project "Client Work" --title "Prepare proposal" \
  --reference EXT-42 --priority high --tag now \
  --scheduled 2026-09-01 --time 09:30 --duration-minutes 30 \
  --content "Review scope" --subtask "Draft proposal"
```

For agent-oriented batches, pass a strict JSON document through a file or
standard input:

```json
{
  "project": "Client Work",
  "tasks": [
    {
      "title": "Prepare proposal",
      "reference": "EXT-42",
      "priority": "high",
      "tags": ["now", "client"],
      "schedule": { "date": "2026-09-01", "time": "09:30" },
      "duration_minutes": 30,
      "content": "Review scope",
      "subtasks": [
        { "title": "Draft proposal", "completed": false }
      ]
    }
  ]
}
```

```text
wtodo import --file tasks.json
# or
wtodo import --stdin < tasks.json
```

List all configured tasks, including nested subtasks:

```text
wtodo list
wtodo list --project "Client Work"
```

Each invocation targets one project. A batch is validated completely and
written through one atomic Markdown replacement; a failure creates no tasks.
Unknown JSON properties are rejected. Timed schedules must use a quarter-hour
from 06:00 through 21:45 and cannot occupy a slot already used by a configured
task or another task in the batch. Durations are 15-minute multiples from 15
through 960 minutes. Exact duplicate tasks are allowed.

Commands emit one JSON result. Exit code `0` means success, `2` means invalid
arguments or input, and `1` means configuration, project, parsing, conflict, or
write failure. An already-running TUI does not watch files; imported tasks
appear on its next normal catalog reload or launch.

The project sidebar includes a virtual `@today` view directly below `All`.
It gathers tasks scheduled for the current local date from every valid project,
keeps project grouping and the active sort, and combines with the `/` filter.
Completed scheduled tasks remain controlled by `:completed`. Because `@today`
is a temporary view, closing there reopens `All` on the next launch.

Additional virtual views can be declared with `[[sidebar.items]]`. Each item
requires a unique `title`, an AND-combined `query`, and an `order`. Query terms
use `field:value` syntax. Supported fields are `scheduled`, `tag`, `project`,
`text`, and `priority`. Scheduled values accept ISO dates and the editor's
relative expressions (`t`, `t-1`, `t+1`, `w+1`, `mon`, `monday`) plus `<`,
`<=`, `>`, and `>=`;
for example `scheduled:<t` finds overdue work. Orders are `source`, `name`,
`scheduled`, `tags`, `file`, or `priority`, optionally followed by `asc` or
`desc`. Saved views appear below `@today`, aggregate all projects, retain tree
context, combine with the live `/` filter, and continue to use `:completed` for
completed visibility. Relative dates are reevaluated against the current local
date whenever the view is drawn.

The interface uses a shared operational-console design across Todos and Day
Planner: a responsive context header, square panels, uppercase structural
labels, adaptive task columns, and configurable semantic foreground and surface
colors. Wide terminals show navigation, tasks, and inspector;
medium terminals prioritize tasks and inspector with navigation available as a
temporary view; narrow terminals show one focused view at a time.

The `Day Planner` tab uses 15-minute slots from 06:00 through 21:45, displayed
as two stacked task slots beneath each 30-minute time label. A todo can
be scheduled for a whole day with `⏳ YYYY-MM-DD`, or assigned to a quarter-hour
slot with Wolf Todo's `⏰ HH:mm` time before all task markers and the
Obsidian Tasks-compatible `⏳ YYYY-MM-DD` scheduled date, for example
`Prepare proposal ⏰ 09:30 ⏱ 30m #work ⏳ 2026-07-15`. Enter
assigns an unscheduled todo or moves an existing assignment, `u` unschedules,
and `[`/`]` change days, while `T` returns to today. A timed task reserves consecutive slots for its
explicit `⏱ <minutes>m` duration; tasks without one are instantaneous. The
planner's default duration is prefilled only for new tasks created there.
Timed tasks and calendar items may overlap; each is shown as a stable timeline
branch.
All-day todos appear in a separate `ALL DAY` pane. Use the configured pane
bindings (`Tab`/`Shift+Tab` by default) to switch between it and the timeline,
then use `j`/`k` and `g`/`G` to navigate. Enter assigns an unscheduled todo when
the pane is empty or moves the selected all-day todo; `/` opens the filtered
picker to add another, and `a` creates a date-only task. Timed and all-day tasks
can be moved between panes without losing their duration. When Google Calendar
is configured, all-day events and focus/status entries are selectable for
Inspector details but remain read-only, while timed meetings appear in their
slots and warn on overlaps. Scheduled todos show either
`YYYY-MM-DD` or `YYYY-MM-DD HH:mm` in the adaptive `SCHEDULED` column in
the Todos pane. The shared field editor can schedule or unschedule work; use
`t` for today, `t+1` for tomorrow, `w+1` for the same weekday next week, or
`mon`/`monday` for the next future Monday. These normalize to the stored ISO
date. `d`/`D` sort by scheduled date and time.
Existing start and due annotations are
preserved in Markdown but intentionally omitted from the normal UI. The planner
shows responsive details for the selected planner item; `v` hides or
restores the Inspector without disabling the all-day pane. On today, a bright,
full-width `┣━━ NOW` timeline row shows the exact current time and, when
available, the next meeting's duration and name. An active Pomodoro appears
first as `◷ MM:SS · <task>`, followed by the meeting as
`NEXT <duration> · <meeting>`; the Pomodoro portion uses the lime `timer` role
while the rest uses the hot-pink `now` role. The Pomodoro also appears as a
temporary, read-only schedule block and disappears when it stops or completes.
The marker refreshes once per second during timing and once per idle minute
otherwise, without borrowing the panel-border style or deriving the meeting
countdown from scheduled todos. Its unscheduled-todo picker shows several
filterable candidates.
On an occupied slot, `e` or `E`, Ctrl+E, and Space provide the same task editing,
external editing, and completion actions as the Todos tab. Creating with `a`
uses the complete task editor, pre-fills the selected slot, and
requires a schedule. Timeline creation requires date and time; all-day creation
requires only the date. Rescheduling from the Planner editor follows the task
to its new date and planner pane.

In the Todos tab, `a` creates a todo under the chosen project's `## Inbox`.
`e` opens one task editor for title, reference, priority, tags, schedule, notes,
and direct subtasks; `E` is a compatibility alias for the same editor. It uses
one cursor across compact field rows and a source-ordered content outline.
Notes use `•`; open and completed subtasks use `◯` and `✓`. Use `a` to choose
and insert content after the selected item (or append when a field is selected),
`e` or open to edit, `d` to remove, Space to toggle a subtask, and Ctrl+S to save
the entire task in one conflict-safe Markdown write. Notes open in a multiline
bottom text box: Enter inserts a line break, Ctrl+S accepts the note into the
task draft, and Escape cancels the text edit. Removing a subtask with descendants
requires confirmation. Space outside the editor changes the selected task's
Markdown checkbox.

Ctrl+E opens the selected todo's Markdown project at its source line in the
terminal editor named by `$EDITOR`. Wolf Todo waits for the editor, then reloads
the project. Helix, Vim-family editors, and Nano receive their supported line
argument; other editors open the file without a line position. `$EDITOR` must
contain an executable name or path without additional arguments.

Command mode belongs to the application shell: `:q`, `:completed`, and unknown
command feedback work from either Todos or Day Planner. Pomodoro commands also
work from either tab. `:archive` is available only from a selected concrete
Todos project: it moves completed task trees into a sibling archive file such
as `work.archive.md`, leaving completed subtasks under open parents in place.
An active feature
picker, filter, move, or edit form receives input before global commands.
While command mode is active, Tab completes a unique command prefix and cycles
ambiguous matches. In the Todos tab, `:roll-today` changes every incomplete
todo in the selected project whose scheduled date is before today to the
current local date. Scheduled times and other metadata are preserved, nested
subtasks are included, and the project is written atomically. The same action
is available through the command palette and the configured
`roll_project_today` binding (`R` by default). Select a concrete project first;
aggregate and saved views cannot be rolled.
Similarly, aggregate/saved views and Day Planner cannot archive tasks; select a
concrete project first.
`?` or `:help` opens the global searchable command palette. Disabled actions
remain visible with a reason; `/` searches and Enter runs the selected action.
In the Todos tab, `v` hides or restores the detail preview for the current
session. Opening a todo restores hidden details automatically.
The Vim-style `g` and `G` bindings jump to the first or last item in the
focused Projects or Todos list, or the first 06:00 and final 21:45 slots in
Day Planner. From an empty Planner slot, `/` opens the unscheduled-todo picker
with its filter active; `T` returns Planner to today.

Use `:move-todo-project <project title>` from the Todos tab to move the
selected todo, including its notes and nested subtasks, into the destination
project's `## Inbox`. Wolf Todo writes the destination before removing the
source, so a failed source write cannot lose the task.

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
