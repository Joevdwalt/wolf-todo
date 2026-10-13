# PLAN0089: Externalize Reusable TUI Renderers

## Status

Implemented.

## Goal

Share generic form, prompt, height, and surface rendering through `Wolf.Controls` while keeping task behavior and Markdown storage in WolfTodo.

## Implementation

- Add render-only `FormDialog` and `TextPrompt` compositions with neutral views and semantic theme roles. WolfTodo keeps task editor view construction, prompt wording, keybindings, and reducers, then maps them to the shared renderers.
- Move the height-constrained and surface decorators to `Wolf.Controls.Rendering`. Keep WolfTodo's `SurfaceThemeRenderer` as the host wrapper.
- Add `Heading` to `ControlTheme` and map it from `TuiTheme` so form labels retain their configured color.
- Show the form and prompt in the standalone gallery. Keep `TodoListItem` and other task-specific renderers in WolfTodo.

## Verification

Cover measurements, semantic rendering, prompt width, height clipping, surface colors, and the WolfTodo adapters with unit tests. Build and run the relevant test projects, then update graphify.
