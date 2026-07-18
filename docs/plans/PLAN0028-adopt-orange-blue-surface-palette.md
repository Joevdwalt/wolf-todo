# PLAN 0028: Adopt Orange and Blue Surface Palette

## Goal

Apply a configurable orange-and-blue palette with dark layered surfaces while
preserving responsive terminal behavior and existing theme configurations.

## Implementation

- Expand the semantic theme with canvas, two surfaces, secondary text, active
  border, bright accent, and information roles.
- Use orange for focus and selection, blue scales for structure and information,
  and Surface 2 for inspectors, overlays, and selected rows.
- Decorate rendered segments so complete allocated cells receive a surface,
  explicit nested surfaces survive parent decoration, and no extra lines are
  emitted.
- Keep Classic and Mono on terminal-default surfaces and accept `default` for
  every new TOML role.

## Verification

- Test preset values, overrides, compatibility, foreground/background HTML,
  selected-row fills, and responsive viewport invariants.
- Run `task test` and `git diff --check`.
