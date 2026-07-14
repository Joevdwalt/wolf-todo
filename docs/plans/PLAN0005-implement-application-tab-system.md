# PLAN 0005: Implement Application Tab System

## Status

Implemented

## Goal

Introduce a reusable fixed-view tab component and typed TUI application shell,
with the existing project browser hosted as the initial `Todos` tab.

## Implementation

1. Add immutable tab definitions and host state, wraparound reducer behavior,
   and a presentation model independent of browser state.
2. Add a typed application shell that owns tab and feature state, routes tab
   shortcuts before normal feature input, and defers entirely to modal input.
3. Render an always-visible, single-line tab strip above the browser while
   preserving the browser's stable minimum height.
4. Generalize browser key bindings into TUI-wide bindings and add configurable
   next/previous tab gestures.
5. Cover component behavior, shell routing, configuration, and terminal output
   with focused tests.
6. Record the shell architecture and tab interaction contract without defining
   the future day planner's storage format.
