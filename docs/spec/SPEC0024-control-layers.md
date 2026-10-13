# SPEC 0024: Control Layers

## Status

Accepted

## Behavior

`Wolf.Controls.Rendering.LayeredRenderable` composes controls on a fixed-size
terminal canvas. Each `ControlLayer` supplies a renderable, zero-based `X` and
`Y` coordinates, a `ZIndex`, and an optional render width. Higher z-index values
cover lower ones; equal values follow input order, with the later layer in front.
Rendered spaces cover content behind them. Cells outside the canvas are clipped.
Uncovered cells are blank, while visible cells retain their foreground,
background, decoration, and link. Replacing part of a wide glyph clears the
whole glyph so the terminal does not display a broken character.

Hosts choose which controls to include and where to place them. Layer order
affects drawing only; the host still routes input to the active control.
