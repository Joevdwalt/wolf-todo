# PLAN0090: Share Task Detail Rendering

## Goal

Reuse task detail presentation across the Todos inspector and Day Planner while preserving each view's output.

## Implementation

- Share task title, identity, metadata, and content rendering; compose planner-specific indicators and calendar details around it.
- Reuse matching metadata for all-day tasks while retaining their distinct schedule label and compact content.
- Preserve recursive browser subtasks, planner direct subtasks, and their existing empty-state styles.

## Verification

Run focused renderer tests and the repository test task, then update graphify.
