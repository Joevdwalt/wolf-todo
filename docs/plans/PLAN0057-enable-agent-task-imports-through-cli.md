# PLAN 0057: Enable Agent Task Imports Through the CLI

## Summary

Make `wtodo` a stable agent-facing writer for the configured Markdown projects
used by `wtodo-tui`, with single-task flags, strict JSON batches, atomic writes,
and structured results.

## Implementation

- Add an all-or-nothing Core batch-creation operation that appends ordered task
  blocks beneath `## Inbox` in one atomic project replacement.
- Add CLI configuration loading, configured title/path resolution, schedule
  collision validation, strict JSON decoding, option parsing, and JSON output.
- Publish `wtodo add` and `wtodo import` without persistent IDs, implicit
  duplicate suppression, or live-TUI synchronization.
- Add an install task and launcher, update usage documentation, and keep the
  knowledge graph current.

## Verification

- Cover Core atomicity, source lines, validation, CLI arguments and JSON,
  project resolution, schedule conflicts, configuration, and physical writes.
- Run `task build`, `task test`, and `graphify update .`.
