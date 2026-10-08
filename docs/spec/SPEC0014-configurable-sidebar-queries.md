# SPEC 0014: Configurable Sidebar Queries

## Status

Accepted

## Purpose

Allow frequently used aggregate task views to be configured beneath `All`
without changing project Markdown or adding application-specific state to it.

## Configuration

Each view is an array-of-table entry:

```toml
[[sidebar.items]]
title = "@today"
query = "scheduled:t"
order = "scheduled asc"
```

Titles must be non-empty and unique case-insensitively. `All` is reserved;
`@today` is an ordinary configurable title.

Queries contain whitespace-separated `field:value` terms combined with AND.
Supported fields are:

- `scheduled`: ISO or relative scheduled date, optionally prefixed by `<`,
  `<=`, `>`, `>=`, or `=`;
- `tag`: an exact tag, with an optional leading `#`;
- `project`: a case-insensitive project-title substring;
- `text`: a case-insensitive title, reference, section, or tag substring; and
- `priority`: `lowest`, `low`, `medium`, `high`, or `highest`, with missing
  priorities treated as low.

Relative dates use `t`, `t+n`, `t-n`, `w+n`, `w-n`, and English weekday names
or abbreviations such as `mon` and `monday`. Weekday expressions resolve to the
next future occurrence and are reevaluated against the local date on every
presentation.

Order accepts `source`, `name`, `scheduled`, `tags`, `file`, or `priority`,
optionally followed by `asc` or `desc`. Ascending is the default.

## Behavior

- Place saved views after `All` and before configured projects, in configuration order.
- Show the count of open todos matching the saved query.
- Aggregate matching todos from every valid Markdown project.
- Show each matching todo's eligible recursive subtasks as tree context, even
  when those subtasks do not match the query. Keep ancestor paths for matching
  descendants without revealing unrelated branches.
- Apply the configured order independently of the session's normal sort.
- Intersect the saved query with the session-only `/` filter. A search match
  reveals its eligible recursive subtasks; a matching descendant retains its
  ancestor path. A descendant of a direct query match remains eligible even
  when it does not match the query itself. Apply completed visibility to every
  row.
- Hide completed rows unless `:completed` is enabled, including descendants
  shown as context. The sidebar count includes only open tasks that directly
  match the saved query.
- Treat saved views as virtual aggregate views for creation and persistence.
- After editing, completing, or rescheduling a todo, remove it from the view if
  it no longer matches.

## Acceptance Scenarios

1. `scheduled:t-1` shows tasks scheduled yesterday and changes with the local
   date.
2. `scheduled:<t` shows overdue scheduled tasks but not today's tasks.
3. Multiple terms must all match.
4. A configured descending scheduled order overrides the session sort.
5. `/report` further narrows a saved view without changing its count.
6. `:completed` reveals completed matches after open matches.
7. A matching task shows eligible recursive descendants; a matching descendant
   under an unmatched parent retains its path without unrelated branches.
8. `/` narrows the saved view while keeping matching rows' eligible descendants
   and ancestor paths; no search matches produces no todo rows.
9. Context descendants do not increase the direct-match sidebar count.
10. Invalid fields, date expressions, orders, duplicate titles, and the reserved
    `All` title fail configuration loading with a useful error.
