# PLAN 0025: Adopt the Operational Console Design

## Status

Implemented

## Goal

Apply the supplied design guide as a coherent, configurable visual foundation
across the Todos browser and Day Planner without changing the Markdown storage
model or inventing unsupported task state.

## Implementation

1. Replace the tab-only strip with a responsive operational header backed by
   real view mode, date, open-count, project-health, and tab-binding data.
2. Introduce adaptive task columns, compact state/priority codes, selected,
   completed, and overdue semantics, and project context in the aggregate view.
3. Make the medium Todos layout task-first with an inspector and temporary
   navigation view; retain wide three-pane and narrow focused-view behavior.
4. Apply square borders, uppercase structural labels, and contextual command
   language to Planner, inspectors, editors, pickers, and menus.
5. Adopt the guide as SPEC0013, align related specifications and README, and
   update deterministic rendering and presenter tests.

## Verification

- Build the complete solution with no warnings.
- Run all CLI, Core, and TUI tests.
- Inspect the final diff for accidental background colors or fabricated data.

