using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Infrastructure;

public sealed record StatusBlock(IRenderable Renderable, int RowCount);
