# RDR 0001: Document Naming Convention

## Status

Accepted

## Context

Repository documentation needs predictable names so that related records are
easy to identify, sort, reference, and create consistently. The repository
already uses `ADR0001-use-dotnet-and-spectre-console.md` for its first
architectural decision record.

## Decision

Numbered documentation records must use this filename format:

```text
{TYPE}{NNNN}-{kebab-case-slug}.md
```

`TYPE` identifies the document category, and `NNNN` is a zero-padded,
four-digit sequence number that is independent for each category. Sequence
numbers begin at `0001`, increment for each new record of that type, and are
not reused.

| Type | Directory | Filename example |
| --- | --- | --- |
| ADR | `docs/adr/` | `ADR0001-use-dotnet-and-spectre-console.md` |
| RDR | `docs/rdr/` | `RDR0001-document-naming-convention.md` |
| PLAN | `docs/plans/` | `PLAN0001-initial-application-plan.md` |
| SPEC | `docs/spec/` | `SPEC0001-todo-file-format.md` |

The slug must be lowercase kebab case and describe the record's subject.

This convention applies to numbered records in `docs/`. Conventional root
guidance files, including `README.md` and `AGENTS.md`, retain their existing
names.

New document categories or prefixes require a subsequent RDR before use.

## Consequences

### Positive

- Documentation is consistently sorted and identifiable by type and sequence.
- Filenames reveal both the record category and its subject.
- Independent sequences avoid coupling unrelated document categories.
- Existing root conventions remain compatible with common repository tooling.

### Negative

- Contributors must determine the next available number before adding a record.
- Adding a new documentation category requires an explicit repository decision.

## References

- [Project README](../../README.md)
- [ADR0001: Use .NET and Spectre.Console](../adr/ADR0001-use-dotnet-and-spectre-console.md)
