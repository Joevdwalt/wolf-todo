# PLAN0081: Integrate SplashBox into TUI Startup

## Status

Implemented

## Summary

Replace the TUI's one-shot static startup splash with the reusable
`Wolf.Controls.SplashBox`, retaining the project-owned ASCII wolf and semantic
theme colors. The splash animates at startup for 800 ms and consumes the
dismissal key.

## Delivered

- Added optional logo content to `SplashBoxState`.
- Hosted the control through `AnsiConsole.Live` with host-timed frame updates.
- Set splash redraws to approximately 60 FPS for a smoother live-terminal transition.
- Preserved immediate any-key dismissal and the undersized-terminal fallback.
- Mapped `TuiTheme` roles into `ControlTheme` and updated startup tests.
