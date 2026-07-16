# SPEC 0012: Global Command Palette

## Status

Accepted

## Purpose

Make configured commands and feature actions discoverable and executable from
either application tab.

## Behavior

The configurable `?` gesture or `:help` command opens a shell-owned bottom
panel. The palette lists Application, Todos, and Planner actions with their
resolved command or shortest configured gesture. Unavailable actions remain
visible with a reason and cannot execute.

The Todos details action is labeled `Hide details` or `Show details` from the
current browser state and executes the same semantic toggle as its binding.
The palette also exposes typed `Jump to top` and `Jump to bottom` Todos actions
with their resolved bindings. `Edit in $EDITOR` is enabled only when the Todos
tab has a selected todo.

Configured movement changes selection. `/` starts search across group, label,
description, and binding. Enter/open executes an enabled typed action. Escape
clears an active query before closing the palette. Once open, the palette
captures input before tab and feature routing.

The palette uses the normal status area and reduces active content height so
the application tab strip remains visible on supported short terminals.

## Acceptance Scenarios

1. `?` and `:help` open the same palette from Todos and Day Planner.
2. Search filters actions and an enabled result executes with Enter.
3. Disabled actions remain visible and explain why they cannot run.
4. Displayed commands and gestures reflect configuration overrides.
5. Palette rendering never scrolls the application tabs off the screen.

## References

- [SPEC0001: Terminal Splash Screen](SPEC0001-terminal-splash-screen.md)
- [SPEC0004: Configurable Browser Key Bindings](SPEC0004-configurable-browser-key-bindings.md)
- [SPEC0005: Application View Tabs](SPEC0005-application-view-tabs.md)
