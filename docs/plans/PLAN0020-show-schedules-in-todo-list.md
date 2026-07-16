# PLAN 0020: Show Schedules in the Todo List

## Status

Implemented

## Implementation

- Render `⏳ YYYY-MM-DD HH:mm` beneath each scheduled Todos-pane row, aligned
  with its title and styled with the semantic date color.
- Fit the todo viewport by logical groups so a task and schedule remain
  together while headings and unscheduled tasks retain their existing height.
- Preserve responsive layout height, scrolling selection, Markdown schedule
  metadata, the Details field, and Day Planner behavior.
