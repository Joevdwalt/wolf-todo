# PLAN 0063: Extract Browser Workflow Collaborator

## Goal

Move browser-specific Markdown mutation coordination out of `TuiApplication`
while retaining the application shell as the owner of global event routing,
tabs, command palette, and timers.

## Scope

- Add `BrowserWorkflow` under `Features/ApplicationShell`.
- Centralize browser transitions, bulk updates, external edits, project moves,
  archival, catalog reloads, and browser feedback state.
- Preserve existing behavior and injectable dependencies.

## Verification

- Run `task build` and `task test`.
- Refresh the AST graph with `graphify update .`.
