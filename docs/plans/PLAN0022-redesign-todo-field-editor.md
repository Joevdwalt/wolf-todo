# PLAN 0022: Redesign the Todo Field Editor

## Status

Implemented

## Implementation

- Render all todo fields as stacked label/value pairs on taller terminals and
  show only the active field on short terminals.
- Preserve configured field navigation, Enter-to-edit behavior, Ctrl+S saving,
  cancellation, validation, and Markdown mutations.
- Show explicit empty values and edit cursors, truncate long values by terminal
  cell width, and account for wrapped hints and errors in footer height.
