using Spectre.Console.Rendering;

namespace Wolf.Controls.Rendering;

public sealed record LayoutChild(IRenderable Content, LayoutSize Size);
