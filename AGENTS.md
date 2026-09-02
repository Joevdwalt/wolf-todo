# Agent Guidance

Read [README.md](README.md) before making changes. It is the source of truth for
the application's purpose, structure, and AI guidance.

## Project Intent

Build a todo manager that stores todos as Markdown files. Keep the Markdown-file
storage model central to implementation decisions.

## On documentation

Do not over elaborate on a point. Write documentation in a compact but still readable way. Also review for duplicate or irrelavant docs that still exist and update

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

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
