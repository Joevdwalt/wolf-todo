# SPEC 0007: Configurable TUI Themes

## Status

Accepted

## Purpose

Define a configurable semantic foreground palette shared by the Wolf Todo
splash screen, tab shell, project browser, and future TUI views.

## Configuration

The optional theme table is part of the global `config.toml`:

```toml
[tui.theme]
preset = "wolf"
accent = "#5FD7FF"
heading = "#FFAF5F"
error = "Red"
text = "default"
```

`preset` is optional and defaults to `wolf`. Preset names are
case-insensitive and must be `wolf`, `classic`, or `mono`. A color override
must be a case-insensitive public Spectre.Console color name, an exact
six-digit hexadecimal value beginning with `#`, or `default`.

Supported override keys are:

| Role | Use |
| --- | --- |
| `text` | Ordinary content |
| `accent` | Selection, focus, active tabs, and active input |
| `heading` | Titles, pane headings, and field labels |
| `border` | Pane and status-panel borders |
| `muted` | Secondary counts, hints, and empty states |
| `success` | Successful transient feedback |
| `warning` | High-priority and caution state |
| `error` | Diagnostics and highest-priority state |
| `tag` | Todo tags |
| `date` | Scheduled dates and times |

An override replaces only its corresponding preset color. Unknown keys,
unknown presets, non-string values, malformed hexadecimal colors, and unknown
named colors are startup errors. Theme configuration is read only at startup;
editing the file while Wolf Todo runs has no effect until restart.

## Built-In Presets

The default `wolf` preset uses:

| Role | Color |
| --- | --- |
| `text` | `default` |
| `accent` | `#5FD7FF` |
| `heading` | `#FFAF5F` |
| `border` | `#5F87AF` |
| `muted` | `#808080` |
| `success` | `#5FD787` |
| `warning` | `#FFD75F` |
| `error` | `#FF5F5F` |
| `tag` | `#5FD7AF` |
| `date` | `#87AFFF` |

The `classic` preset uses the terminal foreground for all roles except cyan
for `accent` and red for `error`. The `mono` preset uses the terminal
foreground for every role.

## Rendering Rules

- Apply the resolved theme to the splash, tabs, pane headers and borders,
  project and todo rows, detail fields, metadata, empty states, diagnostics,
  sort/filter/command states, and status panel.
- Configure foregrounds only; do not set terminal background colors.
- Use square borders throughout the interactive application.
- Render completed todo rows with dim `muted`; reserve `success` for explicit
  successful feedback so completed work does not dominate active work.
- Keep bold and dim decorations fixed so hierarchy and state do not depend on
  a user's color choices.
- In the todo field form, render labels with bold `heading`, inactive values
  with `text`, the selected value with bold `accent`, empty placeholders and
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
6. `mono` retains bold and dim hierarchy without setting colored foregrounds.
7. The full and compact todo field forms use the same semantic roles while
   other bottom-panel modes retain their established theme styling.

## References

- [ADR0004: Use a Global TOML Configuration](../adr/ADR0004-use-a-global-toml-configuration.md)
- [ADR0008: Use Semantic TUI Themes](../adr/ADR0008-use-semantic-tui-themes.md)
- [SPEC0001: Terminal Splash Screen](SPEC0001-terminal-splash-screen.md)
- [SPEC0002: Project Browser and Markdown Todo Format](SPEC0002-project-browser-and-markdown-todo-format.md)
- [SPEC0013: Operational Console Design System](SPEC0013-operational-console-design-system.md)
