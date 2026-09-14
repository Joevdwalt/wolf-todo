# SPEC 0023: Task Links

## Status

Accepted

## Purpose

Generate a task code that can be shared and opened inside Wolf Todo. Codes
address Markdown locations using the existing canonical project path and
one-based source line; they do not introduce persistent task IDs.

## Code and Resolution

A code is `wt1-` followed by the first 8 lowercase hexadecimal characters of
the SHA-256 hash of the UTF-8 canonical configured project path, one LF newline,
and the decimal
source line without leading zeros or a trailing newline. Use the same path
canonicalization as project discovery. Project display titles are not inputs.

Generate and resolve codes against the current loaded configured catalog.
Resolution computes codes for every task in valid projects, including nested
and completed tasks. Unconfigured files and error projects cannot resolve.
A code matching multiple locations is ambiguous: reject generation and opening
with an actionable message instead of selecting a task. An unresolved code
reports that no task exists at the linked location; a code with invalid syntax reports the expected `wt1-` plus 8 lowercase hex digits.

Codes identify locations, not enduring tasks. If another task occupies the
original line, the old code opens that task. Inserting or removing lines can
change a task's code; moving or renaming its project file also changes it.
Changing a task's title, metadata, or completion state without moving its line
does not change its code. Codes are local to matching configured file paths
and are not guaranteed to work on another machine.

Generation and resolution do not write Markdown, configuration, IDs, or a link
registry. A code is not an authorization token. Obsidian project-note links in
SPEC0015 remain a separate export feature.

## CLI Access

`wtodo list` and `wtodo list --project <title|absolute-path>` include a
`task_code` string on every task, including completed tasks and nested subtasks.
It uses the same canonical project path and source line as the inspector and
`:task-link`. Pass this value to `:open-task` or `wtodo-tui --open-task`.
Listing remains read-only; opening rejects ambiguous codes as described above.

## Inspector Display

Todos and Day Planner inspectors show `LINK: <code>` for the selected Markdown
root task or subtask, using its actual canonical project path even in aggregate
views. Include it in timed, all-day, multiday, and compact planner details;
compact details put the link first so it remains visible when text is truncated.
Calendar-only items, empty selections, and project errors have no task link.
The value is the same location code shown by `:task-link`.

## Generate a Code

`:task-link` displays the selected task's full code in a selectable text panel.
Show the project title and source line alongside it and explain that it links
to the current location. Escape closes the panel and restores the prior view.

Use the selected todo in Todos, the selected Markdown task in Day Planner, or
the highlighted root or subtask in task focus mode. Calendar-only items and
empty selections report that a Markdown task must be selected. Generation
does not navigate or start a timer.

The command palette exposes `Generate task link`, disabled with a reason when
there is no eligible selection. No default keyboard shortcut, automatic
clipboard operation, or operating-system URL handler is required.

## Open a Code

`:open-task <code>` opens a code from either tab or task focus mode. The palette
action `Open task link` prompts for a code; Escape cancels without navigation.
Both command names participate in shell command completion.

Successful resolution exits task focus mode, opens Todos, selects the concrete
project and exact task, clears the live filter, and focuses the Todos pane.
Keep the current sort and reveal completed tasks when needed to expose the
target and its ancestor path. Scroll the selected row into view using the
normal responsive browser layout. Opening a code does not enter task focus
mode, start timing, or edit the task.

Invalid or unresolved codes leave the current view and selection unchanged
and show an actionable status message. Never fall back to another task.

`wtodo-tui --open-task <code>` applies the same navigation after normal
configuration, catalog, and session-state loading. A successful link overrides
the restored initial project and task selection while retaining the restored
sort. Invalid or unresolved codes retain normal startup behavior and display
the error. No link-specific state is persisted; normal project and sort
persistence still applies when the application exits.

## Acceptance Scenarios

1. Repeated generation for the same canonical path and source line produces
   the same code; different paths or lines normally produce distinct codes.
2. Codes can be generated for root tasks and nested subtasks from Todos,
   Day Planner, and focus mode, including completed tasks when selected.
3. Calendar-only and empty selections disable generation with a reason.
4. Command and palette opening select the exact task in its concrete project,
   clear filtering, reveal hidden completed ancestors when needed, and scroll
   correctly at wide, medium, narrow, and short terminal sizes.
5. Opening from focus mode exits focus; generating a code preserves it.
6. Startup opening overrides saved project selection but retains saved sort;
   failed resolution preserves normal startup and reports the error.
7. Malformed codes and locations without a task report errors without
   navigation. Unconfigured and invalid projects cannot resolve.
8. A task moved to another line or project receives a different code. An old
   code opens a different task now occupying its original location.
9. Title, metadata, and completion changes on the same line preserve the code;
   generation and resolution leave Markdown and configuration unchanged.
10. Palette prompting can be cancelled, and both commands appear in completion.
11. Colliding short codes reject generation and opening without navigation.
12. Both inspectors show the selected task's short code, including nested tasks
    in aggregate views and timed/all-day planner tasks. Calendar-only, empty,
    and error selections show no task link.
13. CLI listing includes the same short code for root tasks, completed tasks,
    and nested subtasks, both across projects and with a project filter.

## References

- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0012: Global Command Palette](SPEC0012-global-command-palette.md)
- [SPEC0015: Day Schedule Markdown Export](SPEC0015-day-schedule-markdown-export.md)
- [SPEC0022: Task Focus Mode](SPEC0022-task-focus-mode.md)
- [ADR0007: Persist TUI Session State Separately](../adr/ADR0007-persist-tui-session-state-separately.md)
