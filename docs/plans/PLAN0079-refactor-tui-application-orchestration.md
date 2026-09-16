# PLAN 0079: Refactor TUI Application Orchestration

## Goal

Reduce `TuiApplication` complexity while keeping the typed shell, Markdown
workflows, reload behavior, timers, and input precedence unchanged.

## Implementation

- Keep `TuiApplication` responsible for startup, the application loop, reload,
  shutdown, and session persistence.
- Move frame composition and terminal rendering into `ApplicationFrameCoordinator`.
- Move command-mode execution into `ApplicationCommandDispatcher` and command
  palette execution into `ApplicationPaletteDispatcher`.
- Move transient-overlay, global-key, tab, and feature routing into
  `ApplicationInputDispatcher`, passing immutable state and view context.
- Preserve the existing constructor injection points while allowing each
  collaborator to be supplied by focused tests.

## Verification

- Keep the existing shell acceptance tests as regression coverage for command,
  palette, timer, focus, planner, reload, and persistence behavior.
- Run `task build`, `task test`, `git diff --check`, and `graphify update .`.
