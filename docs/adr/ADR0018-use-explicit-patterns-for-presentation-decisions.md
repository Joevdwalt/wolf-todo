# ADR 0018: Use Explicit Patterns for Presentation Decisions

## Status

Accepted

## Context

Presentation code often maps several state values to a glyph, color, or
decoration. Nested conditional operators make precedence harder to see, while
discard patterns hide which state values a rule considers.

## Decision

When presentation output depends on several state values, use a switch
expression with explicit cases or guards instead of nested conditional
operators. Do not use `_` discard patterns to ignore values in these mappings;
name the values or enumerate the relevant cases. Language-required discards,
such as `out _`, remain allowed as described in ADR0003.

## Consequences

- Each presentation rule and its priority are visible together.
- Switches may need more explicit arms when several state combinations exist.

## References

- [ADR0003: Structure Source Code for Testability](ADR0003-structure-source-code-for-testability.md)
