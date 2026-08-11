# PLAN 0050: Show Meeting Names and Highlight NOW

## Goal

Show the next meeting name beside its countdown and make the live Day Planner
marker unmistakable with a dedicated hot-pink theme role.

## Implementation

- Carry the selected future calendar event title on the logical NOW row.
- Render `NOW · <duration> · <title>`; normalize title whitespace, preserve
  the duration first, and ellipsize a long title without wrapping.
- Add a configurable `now` theme color, defaulting to `#FF5CA8` in Wolf,
  magenta in Classic, and terminal default in Mono.
- Apply the new foreground role to the NOW time and bar without adding a
  background fill.
- Document the theme and planner behavior.

## Verification

- Test title selection, text layout, narrow-width ellipsis, and configured NOW
  coloring.
- Test the new theme defaults and TOML override.
- Run `task test`, `git diff --check`, and `graphify update .`.
