# PLAN 0003: Stabilize Browser Height During Filtering

## Status

Implemented

## Summary

Give every project-browser layout a terminal-relative minimum height so a
short filtered result set does not pull the status panel upward.

## Changes

- Calculate the pane-content minimum from the current terminal height while
  reserving space for table chrome and the status panel.
- Track each pane's logical rendered lines and pad the table with empty rows
  until it reaches that minimum.
- Apply the behavior to wide, medium, and narrow layouts on every render.
- Preserve expansion for content taller than the terminal; do not introduce
  cropping or scrolling.

## Verification

- Compare status-panel positions before and after filtering in every responsive
  layout.
- Confirm a result set taller than the minimum still renders its final row.
- Run the repository build and test tasks.
