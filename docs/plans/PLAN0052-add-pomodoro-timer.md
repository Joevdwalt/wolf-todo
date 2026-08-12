# PLAN 0052: Add Pomodoro Timer

- Extend the active timer with a configurable focus countdown and completion state.
- Prompt shortcut and palette starts for 1–960 minutes, preferring selected task duration.
- Add `:pomodoro <minutes|task> [--untracked]` for immediate starts.
- Ring the terminal bell once at completion and log only task-linked sessions.
- Show remaining time in the shared pulsing timer row across Todos and Planner.
- Render active Pomodoros as temporary read-only planner blocks.
- Add a lime `◷` countdown and task title before hot-pink meeting data in the NOW marker.
- Verify prompting, commands, planner rendering, countdown completion, logging, and bell behavior.
