# Refactor TUI Application Complexity

`TuiApplication` remains a delegating façade and `ApplicationRunner` now owns
only startup/session coordination. The former `Run` decision tree is split
across:

- `ApplicationStartup` for configuration, catalog, and persisted-state loading;
- `ApplicationSession` for cursor, splash, persistence, and the render/input loop;
- `ApplicationFrameRenderer` for browser/planner presentation;
- `ApplicationInputDispatcher` for command, palette, tab, browser, and planner routing;
- `ApplicationTransitionExecutor` for Markdown mutations, external editing,
  project moves, planner state following, and export; and
- immutable runtime, frame, startup, and input-result records.

The composition root assembles these collaborators through
`ApplicationRuntimeComposition`. `ApplicationRunner.Run()` has one startup
decision and delegates the interactive lifecycle. Existing commands,
keybindings, Markdown writes, calendar behavior, and session persistence remain
unchanged.
