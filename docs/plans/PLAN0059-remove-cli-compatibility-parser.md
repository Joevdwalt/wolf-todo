# PLAN 0059: Remove Redundant CLI Parsing

Implemented the typed McMaster command migration while retaining a narrow raw
argument pass only for the three ordered add-content options. Scalar command
values, command dispatch, import source validation, task conversion, and list
formatting now run through dedicated handlers and bound command properties.

The old full `ArgumentResolver`/`AddOptions` compatibility parser and runner
interface were removed. Ordered content remains compatible with SPEC0019,
because McMaster groups repeated values by option rather than preserving the
cross-option token order.

Validation: CLI build and 21 CLI tests pass.
