# SPEC 0002: Project Browser and Markdown Todo Format

## Status

Accepted

## Purpose

Define Wolf Todo's first functional screen and its Markdown input format. The
feature is a read-only browser: each Markdown file is one project, projects
contain task-list todos, and a virtual `All` project aggregates active todos
from every valid project.

Creating, editing, completing, and writing changes were outside the original
browser scope. SPEC0010 extends the browser with those writable workflows.
Recurrence and dependencies remain outside this specification.

## Project Discovery

Load the explicit project file paths from the global configuration defined by
ADR0004. Every configured path must use the `.md` extension, compared
case-insensitively. Do not discover other Markdown files from the same
directory.

Canonicalize file paths and load a file only once when it appears more than
once in configuration. Sort valid projects alphabetically by display title,
then by canonical path when titles match.

A missing, malformed, or unreadable configured project file appears as an error
entry in the project sidebar. Errors from one project must not prevent valid
projects from loading.

## Markdown Project Format

Each Markdown file represents one project. It may begin with YAML front matter:

```markdown
---
title: Client Contracts
---
```

Use the non-empty `title` value as the project display title. When front matter
or `title` is absent, use the filename without its `.md` extension. Invalid YAML
or a non-string/empty `title` is a project error rather than a fallback case.
Ignore unrecognized valid front-matter fields in this version.

Markdown headings provide section context. A todo belongs to the closest
preceding heading. Preserve a nested heading path for display, such as
`Renewals / 2026`, but headings are not required.

## Todo Format

A todo is a Markdown task-list item. Recognize `[ ]` as open and `[x]` or `[X]`
as completed. Other checkbox characters are not supported in this version.

The task line has this logical structure:

```text
- [status] [external-reference - ]title [priority] [tags] [start-date] [due-date]
```

Example:

```markdown
## Renewals

- [ ] 134416 - Milas Contract Renewal ⏫ #now 🛫 2026-07-08 📅 2026-07-31
  - Review current contract
  - Update proposal costing
  - [ ] Confirm outstanding issues
```

The optional external reference appears first and ends with ` - `. It may
contain ASCII letters, digits, `.`, `_`, `/`, or `-`, must begin with a letter
or digit, and may not contain whitespace. Examples include `134416` and
`ABC-123`. It is a reference to another system, not a stable Wolf Todo ID.

Recognize this Obsidian Tasks-compatible metadata subset:

| Field | Syntax | Rules |
| --- | --- | --- |
| Tags | `#now`, `#client/renewal` | Standard Markdown hashtags; zero or more. |
| Highest priority | `🔺` | At most one priority marker per todo. |
| High priority | `⏫` | At most one priority marker per todo. |
| Medium priority | `🔼` | At most one priority marker per todo. |
| Low priority | `🔽` | At most one priority marker per todo. |
| Lowest priority | `⏬` | At most one priority marker per todo. |
| Start date | `🛫 YYYY-MM-DD` | Optional, unique, and a valid calendar date. |
| Due date | `📅 YYYY-MM-DD` | Optional, unique, and a valid calendar date. |

Metadata tokens may appear in any order after the descriptive text. Remove
recognized metadata from the displayed title and show it in the row and detail
fields. The remaining title must be non-empty. An unrecognized emoji or text is
part of the title and must be preserved.

Only complete metadata matching the documented syntax is interpreted. An
incomplete date marker such as `📅`, a non-ISO value such as `📅 tomorrow`, or
an invalid calendar date such as `📅 2026-02-30` remains ordinary title text so
one unfinished annotation cannot prevent the project from loading.

Multiple recognized priority/start/due fields or an empty title are malformed
todo metadata. Report the canonical file path, source line, and reason in the
project's error preview. A project containing malformed recognized todo
metadata is an error project and contributes no todos to `All`.

Indented non-checkbox list items and indented paragraphs belong to the parent
todo as notes. Indented task-list items are subtasks and may nest recursively.
Completion does not cascade between a parent and its subtasks. Preserve source
order at every level.

Use the canonical file path and source line as runtime identity. Do not add or
require a persistent Wolf Todo ID.

Scheduled, created, completed, and cancelled dates; recurrence; dependencies;
explicit task IDs; and other Obsidian Tasks fields are not interpreted in this
version. Preserve them as ordinary title or note text.

## Project Browser Layout

After the splash screen, open the Todos tab, restore the most recently selected
configured project and sort, focus the Todos pane, and select its first active
todo. Select the virtual `All` project when there is no saved selection or the
saved project is no longer configured. Use source order when the saved sort is
missing or invalid. The operational header described by SPEC0005 and SPEC0013
appears above the browser. The browser contains a project navigator, a todo
list, an inspector, and a bottom contextual command/status panel.

### Wide Terminals

At 120 or more columns and at least 24 rows, show all three panes:

```text
┌ PROJECTS ──────────┬ TODOS: ALL ──────────────────────┬ DETAILS ────────────────┐
│ > All           12 │ S P TASK             SCHEDULED  │ Milas Contract Renewal  │
│   Client Work    7 │ ○ H Milas Contract   2026-07-15 │ PROJECT: Client Work    │
│                    │                         09:30     │ SCHEDULED: 2026-07-15   │
│   Home           5 │ ○ M Prepare proposal -          │            09:30        │
│ ! Missing source   │                                  │ REFERENCE: 134416       │
│                    │ Home                             │ PRIORITY: High          │
│                    │ ○ - Replace light    -           │                         │
│                    │                                  │                         │
│                    │                                  │ NOTES                   │
│                    │                                  │ • Review contract       │
├────────────────────┴──────────────────────────────────┴─────────────────────────┤
│ j/k navigate  Tab pane  l open  h back  / filter  : command  :completed  :q    │
└─────────────────────────────────────────────────────────────────────────────────┘
```

Allocate at least 20 columns to projects and 36 columns each to todos and
details. Give remaining width to the todo and detail panes. Truncate rows with
an ellipsis rather than wrapping them; allow detail content to wrap.

The configured details toggle, `v` by default, removes the Details column and
gives its width to Todos. Showing it again focuses Details. Visibility begins
enabled each launch and is not persisted.

### Medium Terminals

At 80 through 119 columns and at least 18 rows, show todos and the inspector.
The project navigator becomes a temporary full-width navigation view while it
has focus. Open/back and focus gestures move between navigation, tasks, and
inspector. Hiding details gives the task list the full workspace; opening a todo
restores the inspector.

```text
┌ TASKS // ALL ───────────────────────────┬ INSPECTOR ─────────────────────────────┐
│ S P TASK                      SCHEDULED │ Milas Contract Renewal                 │
│ ○ H Milas Contract Renewal   2026-07-15 09:30                                  │
│                                        │ PRIORITY: High                        │
├────────────────────────────────────────┴────────────────────────────────────────┤
│ j/k NAVIGATE  Tab PANE  l OPEN  h BACK  / FILTER  : COMMAND                    │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Narrow or Short Terminals

Below 80 columns or 18 rows, show one focused pane. The sequence is projects,
todos, then details. Enter drills into the next pane and Esc returns to the
previous pane. The command/status line remains visible when space permits; at
smaller sizes show the concise configured movement, back/open, filter, and
command bindings.

Tab and Shift+Tab skip Details while hidden. Opening a todo restores and
focuses Details in every responsive layout.

## Project and Todo Presentation

The project sidebar begins with `All`, followed by valid projects, then source
and project errors. Show the active-todo count beside `All` and each valid
project. Prefix error entries with `!`.

In `All`, group active todos by project in project sort order. Within a project,
group by heading path and preserve todo source order. In an individual project,
group by heading path and preserve source order. Omit empty headings.

Completed todos are hidden by default. The `:completed` command toggles them
for the current browser session. When visible, show completed todos after open
todos within their original project and heading groups.

Todo title lines use adaptive `S P TASK`, optional `PROJECT`, and optional
`SCHEDULED`
columns as defined by SPEC0013. Nested subtasks use the same state, priority,
then title order. They are always expanded and use Unicode `├─`, `└─`, and `│`
connectors calculated from the visible sibling tree. Keep each title on one
line and truncate overflowing text with an ellipsis. The scheduled column displays `YYYY-MM-DD HH:mm` or `-` and uses
the semantic date color. The detail preview displays the complete title,
external reference, project, heading path, priority, tags, schedule, notes, and
the complete recursive subtask tree using the same connectors. Parsed start and due metadata remain preserved in Markdown but
are not presented or edited by the normal TUI.

Selecting an error entry replaces the todo/detail content with its diagnostic,
including the source path and actionable reason. Duplicate project titles are
disambiguated with their source directory in the detail preview.

Completed rows use muted/dim styling and selection accents the row. Structural headings and inspector labels
are uppercase; task and project values preserve their source case.

## Interaction

- Up and Down, or `k` and `j`, move the selection within the focused pane.
- `g` and `G` jump to the first and last item in the focused project or todo
  list.
- Tab moves focus to the next visible pane; Shift+Tab moves to the previous.
- Enter or `l` selects a project, opens details, or drills into the next narrow
  pane.
- Esc or `h` closes details or returns to the previous pane.
- The configured command-mode gesture enters command mode using the behavior
  defined by SPEC0001.
- `:completed` toggles completed todos.
- The configured quit command, `:q` by default, exits with code `0`.

The splash-dismissal key must still be consumed before browser interaction.
Browser inputs and their Vim-compatible defaults are configurable as defined by
SPEC0004.

Browser colors use the semantic TUI theme defined by SPEC0007. Bold and dim
emphasis remain fixed presentation cues and are not theme settings.

The configured sort gesture, `t` by default, opens the bottom-panel sort dialog
defined by SPEC0006. Sorting changes presentation only and never modifies the
Markdown source order.

## Empty States

- No configured project files: configuration validation fails before rendering.
- No valid project files: show project error entries for every configured path.
- Valid projects but no active todos: show `No active todos`; suggest
  `:completed` when completed todos exist.
- A project or heading with no visible todos: show `No todos in this view`.
- A selected todo with no notes or subtasks: show its parsed fields and
  `No additional details`.
- All configured sources invalid: render the browser with error entries rather
  than returning a startup failure, because the global config itself is valid.

## Acceptance Scenarios

1. Every configured Markdown file appears once as a project; unconfigured files
   in the same directories do not.
2. Project titles use valid YAML front matter and fall back to filenames when
   `title` is absent.
3. The supplied task example yields reference `134416`, high priority, tag
   `now`, start date `2026-07-08`, due date `2026-07-31`, notes, and a subtask.
4. Selecting `All` groups active todos by project and heading in source order.
5. Completed todos are hidden initially and toggled with `:completed`.
6. Wide, medium, and narrow terminals use the layouts and navigation described
   above without losing access to projects, todos, details, or commands.
7. A malformed, missing, or inaccessible project file appears as an error entry
   while valid projects remain browsable.
8. Browsing never modifies project Markdown files.
9. Exiting and restarting restores the selected project by canonical path and
   the last sort; removed or unknown paths fall back to `All`, while a missing
   or invalid sort independently falls back to source order.
10. Every launch opens the Todos tab with the Todos pane focused, even when the
    previous session exited from Day Planner.

## References

- [SPEC0001: Terminal Splash Screen](SPEC0001-terminal-splash-screen.md)
- [SPEC0003: Slash Todo Filter](SPEC0003-slash-todo-filter.md)
- [SPEC0004: Configurable Browser Key Bindings](SPEC0004-configurable-browser-key-bindings.md)
- [SPEC0005: Application View Tabs](SPEC0005-application-view-tabs.md)
- [SPEC0006: Todo Sorting](SPEC0006-todo-sorting.md)
- [SPEC0007: Configurable TUI Themes](SPEC0007-configurable-tui-themes.md)
- [SPEC0008: Todo Scheduling Metadata](SPEC0008-todo-scheduling-metadata.md)
- [SPEC0010: Writable Todo Workflows](SPEC0010-writable-todo-workflows.md)
- [ADR0001: Use .NET and Spectre.Console](../adr/ADR0001-use-dotnet-and-spectre-console.md)
- [ADR0003: Structure Source Code for Testability](../adr/ADR0003-structure-source-code-for-testability.md)
- [ADR0004: Use a Global TOML Configuration](../adr/ADR0004-use-a-global-toml-configuration.md)
- [ADR0007: Persist TUI Session State Separately](../adr/ADR0007-persist-tui-session-state-separately.md)
- [ADR0008: Use Semantic TUI Themes](../adr/ADR0008-use-semantic-tui-themes.md)
- [Obsidian Tasks: Auto-Suggest](https://publish.obsidian.md/tasks/Editing/Auto-Suggest)
