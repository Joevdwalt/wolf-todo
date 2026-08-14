# SPEC 0017: Pomodoro Timer

## Status

Accepted

## Behavior

The `[timer]` configuration supports `pomodoro_minutes` from 1 through 180,
defaulting to 25, and `bell`, defaulting to true. `start_pomodoro` defaults to
`Ctrl+P`; it opens a minutes prompt linked to the selected todo, or untracked
when no todo is selected. `start_untracked_pomodoro` defaults to
`Ctrl+Shift+P` and always prompts without a todo. Both command-palette actions
use the same prompt. A linked prompt prefers the selected todo's explicit
`⏱` duration and otherwise uses `pomodoro_minutes`; an untracked prompt uses
the configured default. The prompt accepts whole minutes from 1 through 960,
starts with Enter, cancels with Escape, and retains invalid input with an error.

Command mode supports immediate starts with `:pomodoro <minutes|task>
[--untracked]`. Numeric values are one-off durations from 1 through 960 and do
not change configuration. `task` requires a selected todo with an explicit
duration and cannot be combined with `--untracked`. A numeric command uses the
selected todo when available unless `--untracked` is present.

Only one stopwatch or Pomodoro may run. Pomodoros display
`POMODORO MM:SS · <todo title>` for linked sessions and omit the title for
untracked sessions. At zero, the application shows a completion banner until
the next keypress, requests a desktop notification, and stops the countdown.
When `bell` is enabled, it plays a platform-native completion sound, falling
back to the terminal bell if the native sound cannot start. A task-linked Pomodoro writes its exact interval to the
weekly Markdown time log; an untracked Pomodoro does not write a log entry.
`Ctrl+T` stops either kind of active timer early. Stopping a linked Pomodoro
records its elapsed time; stopping an untracked Pomodoro discards it.

This version provides one focus countdown at a time. It does not automatically
start breaks, repeat cycles, pause, or complete the linked todo.

While active, every Pomodoro is a temporary, read-only interval in the Day
Planner. It is titled from its linked todo or `Pomodoro` when untracked, uses
the timer theme color, participates only in visual overlap lanes, and is
clipped to the selected date and planner hours. It is excluded from task
editing, conflicts, calendar data, Markdown storage, and schedule exports.

On today's planner, the NOW marker presents active focus before meeting data:
`┣━━ NOW · ◷ MM:SS · <task> · NEXT <duration> · <meeting>`. The clock,
countdown, and optional task title use the timer color; the remaining marker
uses the NOW color. At narrow widths, countdowns are retained before titles,
the meeting title is removed or ellipsized before the task title, and meeting
information is removed before the NOW and Pomodoro countdown.

## Acceptance Scenarios

1. Ctrl+P on a selected todo prompts with its task duration or the configured default.
2. Ctrl+Shift+P prompts for a title-free countdown and creates no Markdown entry.
3. Numeric and `task` commands start immediately with the requested duration.
4. A completed Pomodoro rings the configured terminal bell exactly once.
5. Active Pomodoros appear in the schedule and in the mixed-color `◷` NOW marker.
6. Stopping or completing removes both planner indicators.
7. Starting another Pomodoro is blocked while any timer is active.
8. Completion remains visible as `✓ POMODORO COMPLETE · <duration> · <task>` until the next keypress.
