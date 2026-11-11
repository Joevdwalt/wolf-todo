# PLAN 0096: Collapsible Search Results

## Goal

Allow collapsing branches in live search and saved views without changing normal-view collapse choices.

## Implementation

- Track result collapse choices separately for the selected sidebar view and effective query.
- Reveal matching paths initially; apply explicit branch and all-branch toggles to the result tree.
- Reset choices on query or view changes and project reload; preserve committed choices when canceling filter edits.
- Update browser specifications and user guidance.

## Verification

- Test result collapse, expansion, selection, matching, and isolation from normal-view state.
- Run the project test suite and update the code graph.
