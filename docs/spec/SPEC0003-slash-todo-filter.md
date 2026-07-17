# SPEC 0003: Slash Todo Filter

## Status

Accepted

## Purpose

Define a session-only todo filter for the project browser. Pressing `/` opens a
filter prompt that narrows visible todo rows without modifying project Markdown
files, configuration, or project counts.

## Interaction

- Press the configured filter-mode gesture, `/` by default, outside command
  mode to edit the current filter. Show the prompt as `/` followed by the draft
  text.
- Update results live as printable characters are typed. Backspace removes the
  final character. Reset todo selection to the first visible result row whenever
  the draft changes; this may be an ancestor retained for tree context.
- Enter trims and commits the draft. Submitting an empty draft clears the
  filter. A committed filter remains active while navigating, opening details,
  toggling completed todos, and switching projects.
- Esc cancels editing and restores the previously committed filter.
- While command mode is active, `/` is command text rather than a filter key.

The normal wide and compact status hints include `/ filter`. Outside filter
mode, an active filter is displayed in the status area with a hint that `/`
edits it and an empty submission clears it.

The browser panes must occupy at least the terminal height remaining below the
application tab strip and above the status panel in wide, medium, and narrow
layouts. Filtering to fewer results must not move the status panel upward.
When a pane has more rows than fit in the terminal, show a window containing
the selected row instead of growing the application beyond the viewport.

## Matching and Presentation

Apply the filter to the selected project's todos. When `All` is selected, apply
it across every valid project. Compare one trimmed query as a case-insensitive
ordinal substring against each todo's:

- title;
- external reference;
- tags, accepting matches with or without the leading `#`; and
- section path; and
- scheduled ISO date, `HH:mm` time, or combined `YYYY-MM-DD HH:mm` value.

Do not match project titles, priority, compatibility-only start/due dates,
notes, or other detail text.
Apply completed-todo visibility before filtering. Match every flattened todo
and subtask independently. When a subtask matches, retain its eligible ancestor
path as normal selectable rows so the visible result remains a meaningful tree.
Do not include unrelated siblings or descendants. If completion visibility
hides an ancestor, promote its visible descendants to the nearest visible
level and recalculate their tree connectors.

Apply the active session sort after filtering. Changing or clearing a filter
does not reset the selected sort property or direction.

Only show project and section headings that contain matching rows. Project
active-todo counts remain unfiltered. When no visible row matches, show
`No todos match /<query>`.

## Acceptance Scenarios

1. Typing `/renew` immediately narrows the current view to metadata containing
   `renew`, regardless of case.
2. Enter keeps the filter active during navigation and project changes; editing
   it and submitting an empty prompt restores the unfiltered view.
3. Esc during editing restores the previously committed query.
4. A tag can be found with either `now` or `#now`.
5. A schedule can be found by its date, time, or combined value.
6. A matching nested subtask appears with its visible ancestor path even when
   those ancestors do not match; unrelated branches remain hidden.
7. `:completed` continues to control whether matching completed todos are
   eligible for display.
8. Filtering never modifies project Markdown files.
9. Filtering from many rows to one row keeps the status panel at the same
   terminal-relative position in every responsive layout.

## References

- [SPEC0001: Terminal Splash Screen](SPEC0001-terminal-splash-screen.md)
- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0004: Configurable Browser Key Bindings](SPEC0004-configurable-browser-key-bindings.md)
- [SPEC0006: Todo Sorting](SPEC0006-todo-sorting.md)
- [ADR0003: Structure Source Code for Testability](../adr/ADR0003-structure-source-code-for-testability.md)
