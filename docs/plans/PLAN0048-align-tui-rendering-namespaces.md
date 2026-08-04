# PLAN 0048: Align TUI rendering namespaces

## Summary

Move TUI rendering classes out of `Infrastructure` into feature-aligned
namespaces so source layout matches ADR0003 and ADR0012. Keep infrastructure
focused on side-effect adapters.

## Implementation

- Keep terminal and external adapters under `WolfTodo.Tui.Infrastructure`.
- Move project-browser rendering types under
  `WolfTodo.Tui.Features.ProjectBrowser.Rendering`.
- Move day-planner rendering types under
  `WolfTodo.Tui.Features.DayPlanner.Rendering`.
- Move cross-screen status rendering under
  `WolfTodo.Tui.Features.ApplicationShell.Rendering`.
- Move shared terminal rendering primitives under `WolfTodo.Tui.Rendering`.
- Mirror the moved renderer tests under matching test folders and namespaces.

## Verification

- `dotnet build`
- `dotnet test`

## Notes

This pass is behavior-preserving. Renderer ownership should continue moving
away from `BrowserRenderer` when focused rendering behavior is touched.
