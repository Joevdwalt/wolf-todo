# PLAN0066: Build Wolf.Controls

## Status

Implemented

## Summary

Add a standalone .NET 10, Spectre.Console-native TUI control library. It owns
semantic input, themes, rendering, and host-driven animation contracts without
referencing WolfTodo.

## Delivered

- Single-line textbox and selectable-list controls with typed state and outcomes.
- Neutral semantic `ControlTheme` and terminal-space constraints.
- Host-timed Braille spinner, interpolated progress bar, and transient toast.
- Tests including a test-only adapter that maps WolfTodo themes and keybindings.
- Solution and build-task integration for the independent library and its tests.

## Deferred

Multiline editing, migration of existing WolfTodo controls, NuGet packaging,
and a library-owned terminal render loop remain future work.
