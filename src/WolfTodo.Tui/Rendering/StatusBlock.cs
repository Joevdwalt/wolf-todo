using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Rendering;

public sealed record StatusBlock(IRenderable Renderable, int RowCount);
