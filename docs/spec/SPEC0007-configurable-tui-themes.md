# SPEC 0007: Configurable TUI Themes

## Status

Accepted

## Purpose

Define a configurable semantic foreground and surface palette shared by the Wolf Todo
splash screen, tab shell, project browser, and future TUI views.

## Configuration

The optional theme table is part of the global `config.toml`:

```toml
[tui.theme]
preset = "wolf"
background = "#09121B"
surface = "#101C28"
surface_2 = "#162433"
accent = "#F28C28"
accent_bright = "#FFB14A"
error = "Red"
text = "default"
```

`preset` is optional and defaults to `wolf`. Preset names are
case-insensitive and must be `wolf`, `classic`, or `mono`. A color override
must be a case-insensitive public Spectre.Console color name, an exact
six-digit hexadecimal value beginning with `#`, or `default`.
For foreground roles, `default` uses the terminal foreground. For background
and surface roles, it makes that layer transparent to its enclosing or terminal
background.

Supported override keys are:

| Role | Use |
| --- | --- |
| `text` | Ordinary content |
| `secondary_text` | Metadata, counts, and normal hints |
| `accent` | Selection, focus, active tabs, and active input |
| `accent_bright` | Selected-row text and strongest active emphasis |
| `heading` | Titles, pane headings, and field labels |
| `border` | Pane and status-panel borders |
| `border_active` | Focused workspaces and active overlay borders |
| `muted` | Secondary counts, hints, and empty states |
| `success` | Successful transient feedback |
| `warning` | High-priority and caution state |
| `error` | Diagnostics and highest-priority state |
| `tag` | Todo tags |
| `date` | Scheduled dates and times |
| `info` | Informational metadata and the default date family |
| `background` | Application canvas and operational header |
| `surface` | Normal workspace panes |
| `surface_2` | Inspectors, status panels, dialogs, and selection fills |

An override replaces only its corresponding preset color. Unknown keys,
unknown presets, non-string values, malformed hexadecimal colors, and unknown
named colors are startup errors. Theme configuration is read only at startup;
editing the file while Wolf Todo runs has no effect until restart.

## Built-In Presets

The default `wolf` preset uses:

| Role | Color |
| --- | --- |
| `background` | `#09121B` |
| `surface` | `#101C28` |
| `surface_2` | `#162433` |
| `border` | `#23374A` |
| `border_active` | `#35526B` |
| `text` | `#D8E1E8` |
| `secondary_text` | `#A2B2C1` |
| `muted` | `#6B7C8E` |
| `accent` | `#F28C28` |
| `accent_bright` | `#FFB14A` |
| `heading` | `#FFB14A` |
| `success` | `#6CBF84` |
| `tag` | `#7DB7D8` |
| `warning` | `#E2B64D` |
| `error` | `#D96C6C` |
| `info` | `#5FA8D3` |
| `date` | `#5FA8D3` |

The `classic` preset uses terminal-default surfaces and foregrounds except cyan
active accents and red errors. The `mono` preset uses terminal defaults for
every foreground and surface role.

## Rendering Rules

- Apply the resolved theme to the splash, tabs, pane headers and borders,
  project and todo rows, detail fields, metadata, empty states, diagnostics,
  sort/filter/command states, and status panel.
- Paint complete allocated cells so padding and empty rows use the correct
  surface without changing layout height.
- Use `background` for the header/canvas, `surface` for workspaces, and
  `surface_2` for inspectors, overlays, and selected rows.
- Use bright orange plus the selected-row surface for selection; use the blue
  scale for borders, active borders, information, and scheduled values.
- Use square borders throughout the interactive application.
- Render completed todo rows with dim `muted`; reserve `success` for explicit
  successful feedback so completed work does not dominate active work.
- Keep bold and dim decorations fixed so hierarchy and state do not depend on
  a user's color choices.
- In the todo field form, render labels with bold `heading`, inactive values
  with `secondary_text`, the selected value with bold `accent_bright`, empty placeholders and
  navigation hints with dim `muted`, and validation errors with bold `error`.
- Escape user-controlled text before composing styled markup.
- Let Spectre.Console degrade colors to the terminal's supported capability.
- A startup configuration error may use the renderer's fixed error styling
  because no valid theme is available.

## Acceptance Scenarios

1. A configuration without `[tui.theme]` renders the `wolf` preset.
2. Each built-in preset resolves all semantic roles.
3. Named, hexadecimal, and `default` overrides replace only the selected
   preset roles.
4. Invalid presets, keys, or color values fail before interactive rendering.
5. The configured semantic colors appear in both the splash and browser.
6. `mono` retains bold and dim hierarchy without setting foregrounds or backgrounds.
7. The full and compact todo field forms use the same semantic roles while
   other bottom-panel modes retain their established theme styling.
8. Legacy theme configurations load unchanged and every new role supports an
   independent override or `default`.
9. Surface rendering fills rows without increasing viewport height or scrolling
   the operational header.

## References

- [ADR0004: Use a Global TOML Configuration](../adr/ADR0004-use-a-global-toml-configuration.md)
- [ADR0008: Use Semantic TUI Themes](../adr/ADR0008-use-semantic-tui-themes.md)
- [ADR0011: Use Configurable TUI Surfaces](../adr/ADR0011-use-configurable-tui-surfaces.md)
- [SPEC0001: Terminal Splash Screen](SPEC0001-terminal-splash-screen.md)
- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0013: Operational Console Design System](SPEC0013-operational-console-design-system.md)
