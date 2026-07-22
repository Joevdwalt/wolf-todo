# PLAN 0041: Add Configurable Sidebar Queries

## Goal

Generalize the virtual sidebar pattern behind `@today` so recurring aggregate
views such as yesterday's work can be configured with a query and stable order.

## Implementation

- Load validated `[[sidebar.items]]` entries from global TOML configuration.
- Parse AND-combined saved query terms, including relative scheduled dates.
- Insert saved views after `@today` and before Markdown projects.
- Evaluate counts and visible task forests across all projects while retaining
  ancestor context.
- Apply each view's configured order, then intersect with the existing slash
  filter and completed visibility.
- Keep virtual views session-only so project persistence continues to fall back
  to `All`.

## Verification

- Cover query parsing, relative dates, configuration validation, sidebar order,
  configured task order, completed visibility, and slash-filter intersection.
- Run `task test` and `git diff --check`.
