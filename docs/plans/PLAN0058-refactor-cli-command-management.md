# PLAN 0058: Refactor CLI Command Management

## Summary

Use McMaster.Extensions.CommandLineUtils to declare and dispatch the `wtodo`
CLI commands while preserving the existing structured-output and Markdown
workflow contract.

## Implementation

- Add McMaster.Extensions.CommandLineUtils 5.1.0 to the CLI host.
- Declare root, add, import, and list command models with attributes.
- Keep the generic host and constructor injection for command dependencies.
- Preserve legacy help text, aliases, exit codes, JSON errors, and ordered
  repeated content options through the CLI runner compatibility boundary.
- Remove the unused enum and command marker abstractions; retain the existing
  option-to-task compatibility parser until command execution is migrated to
  typed option models in a follow-up change.

## Verification

- Cover help aliases, unknown commands, command execution, and interleaved
  content options in CLI tests.
- Run `task build`, `task test`, and `graphify update .`.
