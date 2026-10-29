# PLAN 0091: Collapse Subtasks in Todos

## Goal

Let users collapse individual task branches or all branches in the selected
Todos sidebar view, without changing Markdown storage.

## Implementation

- Add session-only collapsed task identities to browser state. Apply them after
  query, filter, completion, and sort selection; preserve selection on a visible
  ancestor when a branch closes.
- Bind `c` to the selected branch and `C` to all branches in the selected view.
  Show branch indicators, status hints, and command-palette actions.
- Reveal matching descendants during search or in saved views. Opening a task
  link expands its ancestors. Clear collapse state when project files reload.
- Keep the inspector and focus card complete. Update README and relevant specs.

## Verification

- Test nested and bulk collapse, selection, search, saved views, keybindings,
  task links, and narrow rendering. Run `task test` and `git diff --check`.
