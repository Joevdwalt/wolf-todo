# PLAN 0064: Extract Timer Workflow Collaborator

## Goal

Move timer and Pomodoro lifecycle logic from `TuiApplication` into a dedicated
application-shell collaborator shared by the browser and day planner.

## Scope

- Add `TimerWorkflow` under `Features/ApplicationShell`.
- Centralize timer start/stop, Pomodoro prompts, completion, notification,
  time-log recording, status formatting, and planner focus blocks.
- Preserve `TuiApplication` as the global input and tab-routing loop.

## Verification

- Run `task build` and `task test`.
- Refresh the AST graph with `graphify update .`.
