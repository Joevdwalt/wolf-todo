# Open configuration command

Add a global `:config` command and palette action that opens the resolved
`config.toml` in `$EDITOR`. Reuse the existing external-editor lifecycle,
including terminal suspension, waiting, error feedback, and runtime reload.

- Add the command catalog entry, reducer operation, and palette action.
- Launch the global configuration at line one from the application shell.
- Cover command parsing, completion, editor invocation, and terminal lifecycle.
