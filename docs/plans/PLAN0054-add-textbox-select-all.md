# PLAN 0054: Add Textbox Select All

## Status

Implemented

## Summary

Give every focused editable textbox desktop-style Ctrl+A selection, replacement,
deletion, navigation, and visual feedback.

## Changes

- Track a selection anchor alongside the cursor in single-line and multiline
  textbox state.
- Select the complete value with Ctrl+A and apply editing operations to that
  selection before returning to cursor-only editing.
- Render selected characters with a high-contrast theme-derived style while
  retaining the active-end viewport behavior.
- Keep read-only controls, save/cancel outcomes, clipboard behavior, and mouse
  input unchanged.
- Document the shared selection behavior in the writable-workflow and structured
  content-editor specifications.

## Verification

- Cover selection bounds, replacement, deletion, navigation, rendering, and
  read-only behavior with focused control tests.
- Build the solution, run all tests, and update the graphify knowledge graph.
