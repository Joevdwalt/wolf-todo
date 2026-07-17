# SPEC 0006: Todo Sorting

## Status

Accepted

## Purpose

Define property sorting for visible todos without modifying the project
Markdown files or their source order.

## Interaction

Press the configured sort-mode gesture, `t` by default, outside another modal
input to replace the bottom status panel with a sort dialog:

```text
Sort: n/N name  d/D scheduled  p/P priority  t/T tags  f/F file  o source  Esc cancel
```

Lowercase selects ascending order and uppercase selects descending order. A
recognized option applies immediately and closes the dialog. `o` restores
source order, Esc closes the dialog without changing the active sort, and
unrecognized input leaves the dialog open.

The option letters are fixed controls within the dialog. The launcher is the
configurable `sort_mode` binding. While the dialog is open it captures input,
including application-tab gestures. On narrow terminals, split the options
across multiple bottom-panel rows and reduce pane height so the tab strip and
footer remain visible.

Show the selected property and direction in the normal status line. The sort
remains active while navigating projects, filtering, and toggling completed
todos. Save it as best-effort application session state and restore it on the
next launch. A missing or invalid saved sort uses source order.

## Ordering

- Preserve project and section groups. Keep section groups in Markdown order.
- Sort sibling todo blocks recursively so a visible parent retains its subtask
  block. Recalculate Unicode tree connectors from the sorted, filtered sibling
  positions. Filtered descendant matches retain their visible ancestor path.
- Keep open sibling blocks before completed sibling blocks when completed todos
  are visible. Equal values retain Markdown source order.
- Compare names case-insensitively using natural numeric chunks, so `Task 2`
  precedes `Task 10`.
- Compare scheduled values chronologically by date and then time. Unscheduled
  todos appear last in both directions.
- Compare priorities by severity. Ascending orders Lowest through Highest;
  descending orders Highest through Lowest. Todos without priority appear last
  in both directions.
- For tags, case-insensitively deduplicate and naturally order each todo's tags,
  then compare the normalized tag sets. Untagged todos appear last in both
  directions.
- File sorting affects project groups in `All`. Compare Markdown basenames
  naturally, then canonical paths as a deterministic tie-breaker. Within one
  file, retain source order.

Apply completed visibility and filtering before presenting the ordered result.
When a sort changes, preserve the selected todo by canonical project path and
source line. Select the first visible todo if that identity is no longer
available.

## Acceptance Scenarios

1. Pressing `t`, then `n` orders `Task 2` before `Task 10`; `N` reverses them.
2. `d` and `D` order scheduled todos in the requested direction and keep
   unscheduled todos last. Existing persisted numeric start-date sorts map to
   scheduled sorting because the enum position is retained.
3. `p` orders Lowest through Highest and `P` reverses that order; both keep
   unprioritized todos last.
4. `t` and `T` inside the dialog order normalized tag sets and keep untagged
   todos last.
5. `f` and `F` order `All` project groups by Markdown filename.
6. `o` restores existing source-order presentation without changing Markdown.
7. Sorting preserves the selected todo, structural groups, and nested blocks.
8. Filter, completed visibility, responsive height, and tab navigation continue
   to follow their existing specifications.
9. Restarting restores the last selected property and direction; legacy state
   without a sort and invalid sort values use source order.

## References

- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0003: Slash Todo Filter](SPEC0003-slash-todo-filter.md)
- [SPEC0004: Configurable Browser Key Bindings](SPEC0004-configurable-browser-key-bindings.md)
- [ADR0005: Use Configurable Browser Key Gestures](../adr/ADR0005-use-configurable-browser-key-gestures.md)
- [ADR0007: Persist TUI Session State Separately](../adr/ADR0007-persist-tui-session-state-separately.md)
