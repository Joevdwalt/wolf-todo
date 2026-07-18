# SPEC 0013: Operational Console Design System

## Status

Accepted

## Purpose

Define the shared visual and responsive language for the Todos browser, Day
Planner, editors, pickers, and command surfaces. The interface is a restrained
operational console: precise, compact, calm, and driven only by real task data
and available actions.

## Foundation

- Use the configured semantic canvas and surface roles. A `default` surface
  preserves the user's terminal background.
- Use square, thin borders and compact spacing. Do not use rounded cards,
  shadows, gradients, decorative metrics, or continuous animation.
- Render structural labels and action names in uppercase. Preserve user-entered
  task, project, note, filter, and command text exactly.
- Use the semantic theme roles from SPEC0007. Selection uses the accent; errors
  and conflicts use error; completed work uses muted/dim styling. Do not color
  an entire row to communicate priority or schedule state.

## Operational Header

Every application view begins with one non-wrapping header. At sufficient
width it contains the product name, tabs, current mode, relevant date, open
todo count, project-file health, and configured tab-switch hint. Less important
segments disappear as width contracts, and remaining overflow is ellipsized.
The active tab is bracketed. Counts and health values must come from the loaded
catalog; fake identifiers, sync claims, or system metrics are forbidden.

## Task Rows

The task list uses adaptive columns headed `S P TASK`, followed by `PROJECT`
and `SCHEDULED` when space permits. `PROJECT` appears for the aggregate All view only.
State uses `○` for open and `✓` for completed. Priority uses `!`, `H`, `M`, `L`,
`.`, or `-`. The selected row uses the elevated surface with bright accent text
and bold emphasis; a completed row is muted
and dim. Scheduled values use the date role without coloring the entire row.
Always-expanded subtasks use restrained Unicode `├─`, `└─`, and `│` connectors
inside the adaptive `TASK` column. Connector width participates in truncation
and column layout. Tagged todos add one non-wrapping tag line beneath the title,
aligned after the state, priority, and tree indentation. Tag lines use the
semantic tag role, remain inside `TASK`, and form one scrolling unit with their
title row. Selected tag lines share the elevated selection surface; completed
tag lines are muted and dim.
Inspector field labels and section headings are uppercase; values retain
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
4. Completed work is muted and schedules remain legible without dominating rows.
5. Custom themes preserve hierarchy with colored or terminal-default surfaces.
6. Forms, menus, pickers, and command hints use the same structural language.
