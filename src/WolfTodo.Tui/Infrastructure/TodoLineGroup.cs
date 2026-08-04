using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Infrastructure;

public sealed record TodoLineGroup(IReadOnlyList<IRenderable> Lines, bool IsSelected);
