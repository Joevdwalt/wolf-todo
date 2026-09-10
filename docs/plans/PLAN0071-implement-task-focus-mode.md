# PLAN 0071: Implement Task Focus Mode

## Status

Completed

## Plan

1. Add transient shell-owned focus state for a root todo and highlighted subtree item.
2. Render a responsive centered task card without the normal tabs and workspaces.
3. Route edit, completion, project movement, timer, Pomodoro, command, and palette actions to the highlighted item.
4. Add the configurable `focus_task` binding and document behavior in SPEC0022.
5. Cover browser and planner entry, nested selection, writes, timing, configuration, and rendering with tests.
