# Refactor TUI Application Complexity

`TuiApplication` remains a delegating façade and `ApplicationRunner` owns only
startup/session coordination. The application shell is organized by
responsibility, with folders and namespaces kept in sync:

```text
Features/ApplicationShell/
├── Actions/          global action catalog and dispatch
├── Commands/         command-mode state, reduction, and input handling
├── CommandPalette/   palette state, presentation, reduction, and input handling
├── ExternalEditing/  external-editor boundary and terminal-safe execution
├── Input/            shell routing and tab input
├── Persistence/      persisted session contract and state
├── Runtime/          startup, frame rendering, loop, and composition
├── ApplicationState.cs
├── ApplicationTabs.cs
└── TuiApplication.cs
```

Concrete side-effect adapters live outside the feature model:

```text
Infrastructure/ApplicationShell/
├── ExternalEditing/ProcessExternalEditorLauncher.cs
└── Persistence/JsonApplicationStateStore.cs
```

The former shell-wide transition and input decision trees are split by feature
ownership:

- `BrowserInputHandler` and `BrowserTransitionExecutor` live in
  `Features/ProjectBrowser`;
- `PlannerInputHandler` and `PlannerTransitionExecutor` live in
  `Features/DayPlanner`;
- `ExternalTodoEditorExecutor` owns terminal suspension around external editing;
- `ApplicationCommandInputHandler` owns global command-mode operations;
- `CommandPaletteInputHandler` owns palette reduction and selection;
- `ApplicationActionDispatcher` routes selected global actions;
- `ApplicationTabInputHandler` owns tab movement; and
- `ApplicationInputDispatcher` now chooses only the active input context.

`ApplicationRuntimeComposition` assembles these collaborators. Existing
commands, keybindings, Markdown writes, external editing, calendar behavior,
schedule export, and session persistence remain unchanged. Tests mirror the
new command, palette, input, persistence, and external-editing namespaces.
