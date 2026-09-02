# ADR 0015: Use McMaster CommandLineUtils for CLI Dispatch

## Status

Accepted

## Context

The CLI previously selected commands and recognized their top-level options
with hand-written switches. This duplicated parser responsibilities and made
command help and growth harder to maintain.

## Decision

Use `McMaster.Extensions.CommandLineUtils` attribute models for command
declarations and subcommand dispatch. Keep the existing Microsoft generic host
as the composition root and use constructor injection for Wolf Todo services.

The CLI retains its established agent-facing contract: legacy help aliases,
JSON results on stdout, existing error codes, exit codes, and ordered Markdown
task content. A thin runner boundary translates McMaster parsing into that
contract. The hosting integration package is not required because the current
host already owns dependency injection and lifetime management.

## Consequences

- New commands have focused, discoverable command model types and generated
  parser metadata.
- Compatibility translation remains necessary for structured errors and legacy
  help output.
- McMaster becomes a maintained third-party dependency of the CLI host.
