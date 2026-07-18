# ADR 0008: Use Semantic TUI Themes

## Status

Accepted; the foreground-only restriction is superseded by ADR0011.

## Context

Wolf Todo's terminal interface previously selected colors directly at each
rendering site. That made the interface visually monotone and made future view
components responsible for inventing their own color rules. Users also have
different terminal palettes and accessibility preferences.

The application needs a stable visual vocabulary that can be shared by the
splash screen, tab shell, project browser, and future views without coupling
those components to one fixed palette.

## Decision

Represent TUI colors as a semantic theme with these foreground roles:
`text`, `accent`, `heading`, `border`, `muted`, `success`, `warning`, `error`,
`tag`, and `date`.

Load the theme once at startup from the optional `[tui.theme]` table in the
global TOML configuration. A theme starts from one of three built-in presets:

- `wolf`: the default orange-and-blue palette, extended with surfaces by
  ADR0011.
- `classic`: a restrained palette compatible with the former interface.
- `mono`: the terminal's default foreground for every semantic role.

Each semantic role can override its preset value. Accept case-insensitive
Spectre.Console named colors, exact six-digit `#RRGGBB` values, and the special
value `default`. Reject unknown presets, theme keys, and invalid values as
startup configuration errors.

ADR0011 extends this decision with semantic background and surface roles. Keep bold and dim decorations as fixed
accessibility and hierarchy cues. Delegate conversion to the terminal's
available color capability to Spectre.Console.

## Consequences

### Positive

- Current and future TUI views share a consistent semantic visual language.
- Users can select a complete preset or override only the colors they need.
- The monochrome preset remains usable in terminals where color is unwanted.
- Invalid configuration fails predictably instead of silently producing a
  partially themed interface.

### Negative

- Theme changes require restarting the application.
- The configuration contract depends on Spectre.Console's named color set.
- Surface themes require complete-cell rendering to avoid patchy backgrounds.

## References

- [ADR0001: Use .NET and Spectre.Console](ADR0001-use-dotnet-and-spectre-console.md)
- [ADR0004: Use a Global TOML Configuration](ADR0004-use-a-global-toml-configuration.md)
- [SPEC0007: Configurable TUI Themes](../spec/SPEC0007-configurable-tui-themes.md)
