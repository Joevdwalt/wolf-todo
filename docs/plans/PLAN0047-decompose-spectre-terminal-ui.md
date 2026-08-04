# PLAN 0047: Decompose Spectre Terminal UI

## Status

Implemented

## Summary

Reduce `SpectreTerminalUi` from the central terminal rendering god node into a
thin adapter that owns terminal lifecycle concerns while concrete collaborators
own rendering and input responsibilities.

## Changes

- Keep `ITerminalUi` unchanged for the application shell.
- Move browser rendering behind `BrowserRenderer`.
- Move planner rendering behind `PlannerRenderer`.
- Add `TerminalInputReader` for blocking and timeout key reads.
- Add `SurfaceThemeRenderer` for style and surface renderable helpers used by
  the adapter.
- Add public `TerminalFrame` and `StatusBlock` value types for renderer
  boundaries.
- Add concrete collaborator registrations in `Program.cs` without introducing
  new renderer interfaces.

## Verification

- Build the solution.
- Run the repository test task.
