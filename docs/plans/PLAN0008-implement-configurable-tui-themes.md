# PLAN 0008: Implement Configurable TUI Themes

## Status

Implemented

## Goal

Replace hardcoded TUI foreground colors with a configurable semantic theme
that supports useful defaults, complete presets, and focused user overrides.

## Implementation

1. Add an immutable ten-role theme model and `wolf`, `classic`, and `mono`
   built-in presets.
2. Parse the optional `[tui.theme]` table at startup, applying named,
   hexadecimal, and terminal-default overrides with strict validation.
3. Pass the resolved theme through the application shell to the terminal UI.
4. Apply semantic colors across the splash, tabs, browser panes, todo states,
   metadata, diagnostics, modes, and status panel while retaining fixed bold
   and dim decorations.
5. Cover preset resolution, configuration validation, application plumbing,
   and rendered semantic colors with automated tests.
6. Document the schema, built-in palettes, rendering rules, and restart
   behavior.
