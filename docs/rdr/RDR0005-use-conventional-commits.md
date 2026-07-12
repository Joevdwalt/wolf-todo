# RDR 0005: Use Conventional Commits

## Status

Accepted

## Context

Wolf Todo needs a consistent Git commit-message format so repository history is
easy to scan and can support automated changelogs, release notes, and semantic
versioning in the future.

## Decision

All contributor-authored Git commits must follow the Conventional Commits 1.0.0
specification using this structure:

```text
<type>[optional scope][optional !]: <description>

[optional body]

[optional footer(s)]
```

Use one of these commit types:

- `feat` for a new user-facing capability.
- `fix` for a user-facing defect correction.
- `build` for build-system or dependency changes.
- `chore` for repository maintenance that does not fit another type.
- `ci` for continuous-integration configuration and scripts.
- `docs` for documentation-only changes.
- `perf` for performance improvements.
- `refactor` for production-code changes that neither add a feature nor fix a
  defect.
- `revert` for reverting an earlier commit.
- `style` for formatting changes that do not affect behavior.
- `test` for adding or correcting tests.

The optional scope must be a concise noun identifying the affected area, such
as `tui`, `cli`, `core`, `build`, or `docs`. Write the description in the
imperative mood, keep it concise, and do not end it with a period.

Mark a breaking change by adding `!` before the colon, by adding a
`BREAKING CHANGE:` footer, or both. The footer must explain the impact and any
required migration.

Examples:

```text
feat(tui): add splash screen
fix(cli): return failure code for invalid input
docs(rdr): define commit message convention
feat(core)!: change todo identifier format
```

Automatically generated merge commits created by the Git hosting platform are
exempt. All commits authored directly by contributors or automation must
conform.

## Consequences

### Positive

- Commit history communicates the intent and affected area of each change.
- Tooling can categorize changes and generate release information reliably.
- Breaking changes are visible in both history and automation.

### Negative

- Contributors and automation must format commit messages consistently.
- Some changes require judgment when selecting a type or scope.
- Existing commits are not retroactively brought into compliance.

## References

- [RDR0001: Document Naming Convention](RDR0001-document-naming-convention.md)
- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/)
