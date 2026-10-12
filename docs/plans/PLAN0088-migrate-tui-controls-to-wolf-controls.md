# PLAN0088: Migrate TUI Controls to Wolf.Controls

## Status

Implemented for `wt1-b56873ed`.

## Goal

Use one reusable implementation for text boxes and select lists, and add the
multiline editor to `Wolf.Controls`. Keep task editing, keybindings, and
Markdown persistence in WolfTodo.

## Current state

- `Wolf.Controls` already provides `TextBox`, `SelectList`, semantic input,
  theme, and size contracts. `src/WolfTodo.Tui/Controls/` duplicates these
  controls with app-specific input and rendering.
- The multiline editor exists only in WolfTodo. `PLAN0066` explicitly deferred
  its migration.
- The test-only `WtodoControlAdapter` shows the needed input and theme mapping;
  `SpectreTerminalUi` already maps `TuiTheme` for `SplashBox`.
- `TodoListItem` uses task rows and selection state, so its task behavior belongs
  in the project browser.

## Implementation

1. **Close library behavior gaps.** Compare the two text boxes and select
   lists, then bring needed behavior into `Wolf.Controls`: cursor and selection
   handling, disabled-option navigation, accurate measured height, narrow-width
   rendering, and theme-driven styles. Follow the square-panel and semantic
   color rules in `SPEC0013`. Keep library APIs free of `TuiTheme`,
   `TuiKeyBindings`, and task types.
2. **Add multiline editing to the library.** Move its state, reducer, outcome,
   measurement, and renderer to `Wolf.Controls`, using `ControlInput` and
   `ControlTheme`. Add a semantic newline input so Enter inserts a line while
   the host's save binding accepts the edit. Preserve LF normalization,
   selection replacement, line navigation, viewport scrolling, and configurable
   save hint text.
3. **Add one production host adapter in WolfTodo.** Convert `ConsoleKeyInfo`
   and configured bindings to semantic input, with separate text, multiline,
   and list contexts. Convert `TuiTheme` to `ControlTheme` and constraints once.
   Move the mapping from the test-only adapter and reuse the existing splash
   theme conversion. Keep command execution and validation in feature reducers.
4. **Migrate callers in small groups.** Replace local textbox state and calls
   in task editing, bulk editing, task links, and timer prompts. Then migrate
   picker and palette views, followed by multiline content editing in the
   browser, planner, and focus views. Update the component harness and the
   independent `Wolf.Controls.Examples` gallery to show multiline editing.
5. **Remove duplicate control types after callers migrate.** Delete obsolete
   app-level text/list/multiline implementations, state, outcomes, and the
   redundant component contract if no feature still needs it. Keep
   `TodoListItem` local, with direct feature methods where the old interface
   no longer applies. Keep type/file/namespace
   alignment required by ADR0012.

## Verification

- Exercise library reducer cases for selection, movement, disabled options,
  multiline newline/save/cancel, and boundary sizes.
- Exercise WolfTodo adapters with custom keybindings and themes, then verify
  task editing, pickers, task links, timer prompts, and all three content-editor
  hosts retain their outcomes and layout.
- Build the solution and run its relevant control and TUI tests. Inspect the
  component harness and gallery at narrow and normal terminal widths.
- Confirm a content edit still reaches the existing conflict-safe Markdown
  write path; no storage format or project-file mutation rules change.

## Documentation

`PLAN0066`'s deferred list and the README gallery guidance reflect this change.
