# SPEC 0002: Project Browser and Markdown Todo Format

## Status

Accepted

## Purpose

Define Wolf Todo's first functional screen and its Markdown input format. The
feature is a read-only browser: each Markdown file is one project, projects
contain task-list todos, and a virtual `All` project aggregates active todos
from every valid project.

Creating, editing, completing, filtering, searching, recurrence, dependencies,
and writing changes to Markdown are outside this specification.

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

After the splash screen, focus the virtual `All` project and select its first
active todo. The browser contains a project navigator, a todo list, a detail
preview, and a bottom command/status line.

### Wide Terminals

At 120 or more columns and at least 24 rows, show all three panes:

```text
┌ Projects ──────────┬ Todos: All ──────────────────────┬ Details ────────────────┐
│ > All           12 │ Client Contracts                 │ Milas Contract Renewal  │
│   Client Work    7 │ > [ ] 134416 - Milas... ⏫ #now  │ Project: Client Work    │
│   Home           5 │   [ ] Prepare proposal       🔼  │ Section: Renewals       │
│ ! Missing source   │                                  │ Reference: 134416       │
│                    │ Home                             │ Priority: High          │
│                    │   [ ] Replace bathroom light     │ Start: 2026-07-08       │
│                    │                                  │ Due: 2026-07-31         │
│                    │                                  │                         │
│                    │                                  │ Notes                   │
│                    │                                  │ • Review contract       │
├────────────────────┴──────────────────────────────────┴─────────────────────────┤
│ ↑↓ navigate  Tab pane  Enter select  : command  :completed  :q                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

Allocate at least 20 columns to projects and 36 columns each to todos and
details. Give remaining width to the todo and detail panes. Truncate rows with
an ellipsis rather than wrapping them; allow detail content to wrap.

### Medium Terminals

At 80 through 119 columns and at least 18 rows, show projects and todos. Enter
on a todo opens its details over the todo pane; Esc closes the details.

```text
┌ Projects ──────────┬ Todos: All ────────────────────────────────────────────────┐
│ > All           12 │ Client Contracts                                           │
│   Client Work    7 │ > [ ] 134416 - Milas Contract Renewal ⏫ #now               │
│   Home           5 │   [ ] Prepare proposal 🔼                                  │
│ ! Missing source   │                                                             │
├────────────────────┴─────────────────────────────────────────────────────────────┤
│ ↑↓ navigate  Tab pane  Enter details  : command  :completed  :q                 │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### Narrow or Short Terminals

Below 80 columns or 18 rows, show one focused pane. The sequence is projects,
todos, then details. Enter drills into the next pane and Esc returns to the
previous pane. The command/status line remains visible when space permits; at
smaller sizes show `: commands  Esc back` as the compact hint.

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

Todo rows display status, optional external reference, title, priority, tags,
start date, and due date as space permits. The detail preview displays the
complete title, project, heading path, parsed fields, notes, and nested subtasks.

Selecting an error entry replaces the todo/detail content with its diagnostic,
including the source path and actionable reason. Duplicate project titles are
disambiguated with their source directory in the detail preview.

## Interaction

- Up and Down move the selection within the focused pane.
- Tab moves focus to the next visible pane; Shift+Tab moves to the previous.
- Enter selects a project, opens details, or drills into the next narrow pane.
- Esc closes details, returns to the previous narrow pane, or cancels command
  mode.
- `:` enters command mode using the behavior defined by SPEC0001.
- `:completed` toggles completed todos.
- The configured quit command, `:q` by default, exits with code `0`.

The splash-dismissal key must still be consumed before browser interaction.
Navigation keys are fixed defaults in this version; only the quit command is
configurable.

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

## References

- [SPEC0001: Terminal Splash Screen](SPEC0001-terminal-splash-screen.md)
- [ADR0001: Use .NET and Spectre.Console](../adr/ADR0001-use-dotnet-and-spectre-console.md)
- [ADR0003: Structure Source Code for Testability](../adr/ADR0003-structure-source-code-for-testability.md)
- [ADR0004: Use a Global TOML Configuration](../adr/ADR0004-use-a-global-toml-configuration.md)
- [Obsidian Tasks: Auto-Suggest](https://publish.obsidian.md/tasks/Editing/Auto-Suggest)
