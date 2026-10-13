using Spectre.Console.Rendering;

namespace Wolf.Controls.Rendering;

/// <summary>A renderable placed on a terminal canvas. Higher z-index values appear in front.</summary>
public sealed record ControlLayer(IRenderable Content, int X, int Y, int ZIndex = 0, int? Width = null);
