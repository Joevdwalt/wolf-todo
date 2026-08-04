using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Features.ProjectBrowser.Rendering;

public sealed record TodoLineGroup(IReadOnlyList<IRenderable> Lines, bool IsSelected);
