# Refactor TUI Application Complexity

Reduce `TuiApplication` to a delegating façade and move runtime orchestration into focused application-shell collaborators. Preserve all existing terminal, command, palette, todo mutation, planner, and Markdown workflows. The façade must have cyclomatic complexity below 10; extracted named types follow ADR0003 and ADR0012 and receive mirrored tests.
