# SPEC 0013: Operational Console Design System

## Status

Accepted

## Purpose

Define the shared visual and responsive language for the Todos browser, Day
Planner, editors, pickers, and command surfaces. The interface is a restrained
operational console: precise, compact, calm, and driven only by real task data
and available actions.

## Foundation

- Preserve the user's terminal background and configure foreground colors only.
- Use square, thin borders and compact spacing. Do not use rounded cards,
  shadows, gradients, decorative metrics, or continuous animation.
- Render structural labels and action names in uppercase. Preserve user-entered
  task, project, note, filter, and command text exactly.
- Use the semantic theme roles from SPEC0007. Selection uses the accent; errors,
  conflicts, and overdue dates use error; completed work uses muted/dim styling.
  Do not color an entire row to communicate priority or due state.

## Operational Header

Every application view begins with one non-wrapping header. At sufficient
width it contains the product name, tabs, current mode, relevant date, open
todo count, project-file health, and configured tab-switch hint. Less important
segments disappear as width contracts, and remaining overflow is ellipsized.
The active tab is bracketed. Counts and health values must come from the loaded
catalog; fake identifiers, sync claims, or system metrics are forbidden.

## Task Rows

The task list uses adaptive columns headed `S P TASK`, followed by `PROJECT`
and `DUE` when space permits. `PROJECT` appears for the aggregate All view only.
State uses `○` for open and `✓` for completed. Priority uses `!`, `H`, `M`, `L`,
`.`, or `-`. The selected row is accented and bold; a completed row is muted
and dim. Only an overdue due value receives error styling.

Scheduled metadata remains on a dim second line below and aligned with its task
title. Inspector field labels and section headings are uppercase; values retain
their original case.

## Responsive Layout

- Wide (120 or more columns and at least 24 rows): navigation, task list, and
  inspector are visible together.
- Medium (80 through 119 columns and at least 18 rows): task list and inspector
  share the workspace. Navigation is a temporary full-width view while focused.
  Hiding details gives the task list the whole workspace.
- Narrow or short: show only the focused view. Navigation, task list, and
  inspector remain reachable using the configured focus/open/back actions.

The task list has priority when space is constrained. Rendering must reserve
the final terminal row and keep the header and contextual command panel visible.

## Planner and Dialogs

The planner uses the same header, square borders, uppercase structure, semantic
state styling, and contextual command panel. Timeline rows prefix assigned work
with state and priority. Wide layouts place an `INSPECTOR` beside the timeline;
smaller layouts may show a `SELECTED` summary below it.

Forms, content editors, pickers, sort menus, and the command palette use square
panels, uppercase structural labels, and configured semantic roles. Keyboard
hints always reflect the current mode and configured bindings.

## Out of Scope

Capture, triage, focus, review, active/waiting/blocked/recurring task states,
timers, transient completion animation, and new synchronization behavior are
future features. This design system does not introduce data that the Markdown
model does not currently represent.

## Acceptance Scenarios

1. Todos and Planner render a consistent operational header and square panels.
2. Wide, medium, and narrow layouts keep the primary task or timeline workspace
   usable while making secondary views reachable.
3. Task metadata columns appear only when their widths can remain legible.
4. Completed work is muted and overdue emphasis is limited to its due value.
5. Custom themes preserve hierarchy without setting a terminal background.
6. Forms, menus, pickers, and command hints use the same structural language.

