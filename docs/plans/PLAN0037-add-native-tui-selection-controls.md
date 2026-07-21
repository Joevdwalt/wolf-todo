# PLAN0037: Add Native TUI Selection Controls

Introduce an internal Spectre.Console control library, beginning with a themed
bottom-sheet selection list. The control owns presentation only; existing
reducers retain navigation, filtering, selection, and execution behavior.

The first adoption covers the command palette, planner unscheduled-todo picker,
and task editor project/content-type pickers. The common control presents a
title, optional search text, scrollable options, disabled state, empty state,
and keyboard footer using the configured Wolf Todo theme.
