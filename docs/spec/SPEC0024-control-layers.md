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

`RowLayout` and `ColumnLayout` allocate child regions in order. A `LayoutSize`
is fixed in cells or weighted against the remaining width or height. Nested
renderables use coordinates local to their region. `OverlayLayout.Place` aligns
a renderable inside a canvas with optional dimensions, margins, and z-index.
The resulting layers are clipped by the canvas and composed by
`LayeredRenderable`.

```csharp
var frame = new ColumnLayout(height,
    new(new Header(), LayoutSize.Fixed(3)),
    new(new Workspace(), LayoutSize.Weighted()),
    new(new Status(), LayoutSize.Fixed(2)));
var popup = OverlayLayout.Place(form, width, workspaceHeight,
    OverlayAlignment.Bottom, width: width - 4, height: formHeight, zIndex: 10);
```
