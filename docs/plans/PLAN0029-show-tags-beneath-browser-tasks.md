# PLAN 0029: Show Tags Beneath Browser Tasks

## Goal

Show todo tags directly beneath their task in the browser without changing the
Markdown storage model or reducing access to existing adaptive metadata.

## Implementation

- Add a compact `#work #now` line to tagged todo render groups; leave untagged
  todos on one line.
- Align tags with the corresponding title after state, priority, and Unicode
  tree indentation, preserve continuation bars through the tag-line gutter,
  and truncate them inside the adaptive `TASK` column.
- Use semantic tag, completion, selection, and surface styling consistently.
- Keep each tag line attached to its title when the viewport scrolls.

## Verification

- Cover root and nested alignment, adaptive truncation, semantic styling, and
  grouped viewport behavior.
- Run `task test` and `git diff --check`.
