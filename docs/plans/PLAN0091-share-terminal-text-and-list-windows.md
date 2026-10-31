# PLAN0091: Share Terminal Text and List Windows

## Status

Implemented.

## Changes

- Share plain-text clipping, ellipsis, and column padding in `Wolf.Controls.Rendering.TerminalText` across task rows, prompts, command palette, and planner.
- Preserve Unicode text elements and wide-character cell widths; return empty text when no cells are available.
- Share selection-following logical row windows in `ListViewport` across Todos, all-day planner, and focus view.
- Delegate task-detail tree traversal to the existing row renderer instead of repeating it.
- Keep existing host methods as adapters and preserve planner title whitespace trimming.

## Verification

Cover width boundaries, Unicode, column padding, and selected row windows directly; run the repository tests and refresh graphify.
