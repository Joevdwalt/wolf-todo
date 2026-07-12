# RDR 0002: Run Repository Scripts Through Tasks

## Status

Accepted

## Context

Repository automation must be discoverable and run consistently by developers,
CI, and automation agents. Invoking scripts directly can obscure required
arguments, environment setup, and the supported way to perform common work.
Scripts also need a consistent implementation language and location so they
can be found and maintained predictably.

## Decision

All repository scripts must be run through a named task declared in the root
`TaskFile.yml`.

Repository scripts must be written in PowerShell, use the `.ps1` extension,
and reside in `src/scripts/`.

Script filenames must use lowercase kebab-case action names without a prefix,
such as `build.ps1`, `test.ps1`, and `generate-documentation.ps1`. Each task
must have the same name as the script it invokes; for example, the `build` task
invokes `src/scripts/build.ps1`.

Each script must include concise comment-based help, enable
`Set-StrictMode -Version Latest`, set `$ErrorActionPreference = 'Stop'`, and
declare any inputs through a `param` block.

Tasks are the supported entrypoints for repository automation, including build,
test, formatting, linting, generation, and maintenance operations. Each task
invokes its matching script, and scripts must be executed through their
declared task rather than directly as a normal repository workflow.

When adding or changing a repository script, add or update its corresponding
task in `TaskFile.yml`. Task names and descriptions must make their purpose
clear to developers and automation agents.

## Consequences

### Positive

- Repository workflows have one discoverable, documented entrypoint.
- Developers, CI, and automation agents use the same commands.
- Tasks can consistently manage prerequisites, arguments, and environment
  setup around underlying scripts.
- PowerShell scripts in `src/scripts/` provide a single, predictable location
  and implementation language for repository automation.
- Matching task and script names make automation easy to discover and invoke.
- The PowerShell baseline makes script behavior and error handling consistent.

### Negative

- Adding a script requires maintaining a matching task definition.
- Contributors must use the task runner rather than invoking supported scripts
  directly.
- Repository automation requires a PowerShell-compatible environment.
- Scripts require the prescribed structure even when a simpler implementation
  might otherwise be sufficient.

## References

- [RDR0001: Document Naming Convention](RDR0001-document-naming-convention.md)
