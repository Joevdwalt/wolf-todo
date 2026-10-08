# PLAN 0087: Show Subtasks in Saved Sidebar Views

## Status

Proposed

## Goal

When a task matches a configured `[[sidebar.items]]` query, show its eligible
recursive subtasks in that view, even when those subtasks do not match the query
themselves. Keep the Markdown task tree intact.

## Implementation

1. In `ProjectBrowserPresenter.BuildVisibleForest`, carry a saved-query match
   from a matching task into its descendants, as the `/` search already does.
   Keep ancestor rows for independently matching descendants, without revealing
   unrelated branches of an unmatched ancestor.
2. Apply completion visibility to every row. Keep `@today` limited to tasks
   scheduled today; the saved-query expansion applies only to configured views.
   When `/` is active, narrow the expanded saved-view tree using the existing
   search rules: a matching row reveals its eligible descendants, while a
   matching descendant retains its ancestor path.
3. Preserve configured ordering, project and section headings, selection
   identities, and tree connectors. Keep sidebar counts based on tasks that
   directly match the saved query, so context rows do not inflate the count.
4. Update SPEC0014, SPEC0003, and the brief README description to state the
   saved-view descendant rule and its interaction with `/` and `:completed`.

## Verification

- Add presenter cases for a matching parent with nonmatching children and
  grandchildren; a matching descendant under an unmatched parent; completed
  children; and `/` matches on the parent, one child, or neither.
- Assert row order, identities, tree connectors, and the unchanged direct-match
  sidebar count. Check that `@today` still excludes off-date descendants.
- Run `task test` and `git diff --check` during implementation.
