# PLAN 0035: Add a Today Sidebar View

## Goal

Provide a fast aggregate view of work scheduled for the current date without
changing Markdown storage or introducing another application tab.

## Implementation

- Add `@today` directly below `All` as a distinct virtual project-row kind.
- Aggregate active tasks scheduled for the current local date across valid
  projects, preserving grouping, sorting, filtering, and tree context.
- Continue to use `:completed` for completed visibility and require project
  selection when creating from the aggregate view.
- Use an injectable date provider, date-role styling, and existing responsive
  project navigation.
- Keep the view session-only so exiting from `@today` restores `All` next time.

## Verification

- Test date eligibility, counts, nested context, filtering, sorting, completed
  visibility, rendering, navigation, and persistence fallback.
- Run `task test` and `git diff --check`.
