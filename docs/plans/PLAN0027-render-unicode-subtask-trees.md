# PLAN 0027: Render Unicode Subtask Trees

## Goal

Replace depth-only indentation with an always-expanded Unicode tree in the
Todos list and browser inspector while keeping Markdown storage unchanged.

## Implementation

- Represent each visible todo with an immutable path of sibling-position
  segments and derive depth from that path.
- Build the visible forest after sorting, completion visibility, and filtering.
  Retain eligible ancestor context for matching descendants and promote
  descendants whose completed ancestors are hidden.
- Format paths with `├─`, `└─`, and `│`, include their display width in the
  adaptive task column, and render the inspector's complete descendant tree.
- Keep ancestor context rows selectable and actionable. Do not add collapse
  state, persistence, keybindings, an ASCII fallback, or changes to the Planner
  and structured content editor.

## Verification

- Cover mixed and deep sibling paths, filtered ancestor context, hidden
  completed ancestors, recursive inspector rendering, and responsive columns.
- Run the complete repository test task.
