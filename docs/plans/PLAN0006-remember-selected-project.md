# PLAN 0006: Remember the Selected Project

## Status

Implemented

## Goal

Restore the project selected when the TUI last exited without changing user
configuration or project Markdown files.

## Implementation

1. Add a platform-standard application-state path separate from `config.toml`.
2. Persist only the selected project's canonical path as best-effort JSON.
3. Restore the matching project after catalog loading and fall back to `All`
   for missing, invalid, or stale state.
4. Save the current selection from the application shell during exit cleanup.
5. Cover path resolution, state serialization, restoration, and saving with
   focused tests.
