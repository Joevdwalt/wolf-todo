# SPEC 0014: Configurable Sidebar Queries

## Status

Accepted

## Purpose

Allow frequently used aggregate task views to be configured beneath `@today`
without changing project Markdown or adding application-specific state to it.

## Configuration

Each view is an array-of-table entry:

```toml
[[sidebar.items]]
title = "@yesterday"
query = "scheduled:t-1"
order = "scheduled asc"
```

Titles must be non-empty, unique case-insensitively, and may not use the
reserved `All` or `@today` titles.

Queries contain whitespace-separated `field:value` terms combined with AND.
Supported fields are:

- `scheduled`: ISO or relative scheduled date, optionally prefixed by `<`,
  `<=`, `>`, `>=`, or `=`;
- `tag`: an exact tag, with an optional leading `#`;
- `project`: a case-insensitive project-title substring;
- `text`: a case-insensitive title, reference, section, or tag substring; and
- `priority`: `lowest`, `low`, `medium`, `high`, or `highest`, with missing
  priorities treated as medium.

Relative dates use `t`, `t+n`, `t-n`, `w+n`, and `w-n` and are reevaluated
against the local date on every presentation.

Order accepts `source`, `name`, `scheduled`, `tags`, `file`, or `priority`,
optionally followed by `asc` or `desc`. Ascending is the default.

## Behavior

- Place saved views after `@today` and before configured projects.
- Show the count of open todos matching the saved query.
- Aggregate matching todos from every valid Markdown project.
- Apply the configured order independently of the session's normal sort.
- Retain matching descendants' ancestor paths and omit unrelated branches.
- Intersect the saved query with the session-only `/` filter.
- Hide completed matches unless `:completed` is enabled.
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
7. Invalid fields, date expressions, orders, duplicate titles, and reserved
   titles fail configuration loading with a useful error.
