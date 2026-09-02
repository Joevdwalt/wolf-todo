# PLAN 0060: Default Missing Priority to Low

## Status

Implemented

## Implementation

- Treat todos without an explicit priority as Low when sorting by priority and
  matching saved priority queries.
- Preserve absent priority metadata in Markdown, TUI presentation, and CLI
  output rather than writing or displaying an explicit Low priority.
- Update focused sorting and saved-query coverage plus the governing
  specifications.

Validation: all 601 CLI, Core, and TUI tests pass.
