# Agent Guidance

Read [README.md](README.md) before making changes. It is the source of truth for
the application's purpose, structure, and AI guidance.

## Project Intent

Build a todo manager that stores todos as Markdown files. Keep the Markdown-file
storage model central to implementation decisions.

## Repository Layout

- `src/`: application source code.
- `docs/adr/`: architectural decision records.
- `docs/plans/`: AI-generated implementation plans.
- `docs/spec/`: functional specifications.

## Working Conventions

- Place source code in `src/` and documentation in the appropriate `docs/`
  subdirectory.
- Check relevant specifications and architectural decisions before changing
  behavior or architecture.
- Keep plans created during AI-assisted work in `docs/plans/`.
- Update `README.md` when the project purpose or repository layout changes.
