# PLAN 0031: Unify Task Editing in One Dialog

## Goal

Replace the separate property form and structured content editor with one
bottom-panel task editor shared by Todos and Day Planner.

## Implementation

- Use one state machine and cursor for six editable fields plus the ordered
  notes/subtasks outline.
- Reuse the component for creation and editing, including Planner project and
  schedule requirements.
- Make `e` canonical and retain the configured `E` binding as an alias while
  exposing only one command-palette action.
- Carry fields and content through one update model so conflict validation and
  Markdown replacement happen atomically.
- Keep external Ctrl+E editing, source ordering, subtree removal confirmation,
  theme roles, and responsive viewport behavior intact.

## Verification

- Cover shared Browser/Planner reducer behavior, field and content navigation,
  rendering at supported terminal sizes, aliases, and one-write mutations.
- Run `task test` and `git diff --check`.
