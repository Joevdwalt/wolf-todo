# PLAN 0004: Implement Configurable Vim Key Bindings

## Status

Implemented

## Summary

Add Vim-style `h/j/k/l` browser navigation and replace hardcoded browser inputs
with validated, optional global TOML bindings.

## Changes

- Parse printable characters and named console keys with modifiers into
  immutable gestures.
- Apply compatible defaults for omitted actions and replacement semantics for
  configured arrays.
- Route navigation, pane actions, mode launchers, completed, and quit behavior
  through resolved bindings.
- Generate normal, compact, and active-filter hints from those same bindings.
- Reject malformed, duplicate, and conflicting configuration at startup.

## Verification

- Cover parsing, defaults, overrides, modifiers, conflicts, reducer behavior,
  application propagation, and status hints.
- Run the repository build and test tasks.
