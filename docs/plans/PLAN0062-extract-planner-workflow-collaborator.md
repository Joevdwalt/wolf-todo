# PLAN 0062: Extract Planner Workflow Collaborator

## Goal

Reduce `TuiApplication` orchestration complexity by moving planner-specific
rendering, input reduction, calendar synchronization, export, external edit,
and Markdown mutation coordination behind an application-shell collaborator.

## Scope

- Add `PlannerWorkflow` under `Features/ApplicationShell`.
- Preserve the existing `TuiApplication` constructor dependencies while
  allowing a workflow to be injected for focused tests.
- Keep cross-feature concerns—tab routing, command palette, and timers—in
  `TuiApplication`.

## Verification

- Run `task build` and `task test`.
- Refresh the AST graph with `graphify update .`.
