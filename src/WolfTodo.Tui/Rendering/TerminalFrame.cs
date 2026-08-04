using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Rendering;

public sealed record TerminalFrame(
    IRenderable Header,
    IReadOnlyList<IRenderable> Content,
    StatusBlock Status);
