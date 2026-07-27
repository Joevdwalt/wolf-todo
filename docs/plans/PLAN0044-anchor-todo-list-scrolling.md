# PLAN 0044: Anchor Todo List Scrolling

## Status

Implemented

## Summary

Keep approximately ten rendered terminal rows below the selected todo while
navigating a long Todos-pane list, rather than scrolling only once the selected
task reaches the pane's bottom edge.

## Changes

- Reserve ten visual rows of look-ahead below the selected todo whenever the
  pane has sufficient height.
- Count headings and tag rows by their rendered terminal height and preserve
  complete todo groups.
- On short panes or near the final todo, show as much following content as
  possible and fill unused space above the selection.

## Verification

- Cover early scrolling in a long plain list and a tagged list.
- Run the repository test task.
