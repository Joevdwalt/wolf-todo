# TUI resize reflow

- Poll terminal dimensions in the application loop and request a redraw on width or height changes without consuming input or file events.
- Render the active view from its existing state at the new size and clear the previous frame on resize.
- Keep the splash responsive during and after its animation, including its small-terminal fallback.
- Cover dimension changes and unavailable readings in tests, verify the TUI, and update the specifications and graph.
