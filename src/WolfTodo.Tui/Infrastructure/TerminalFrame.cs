using Spectre.Console.Rendering;

namespace WolfTodo.Tui.Infrastructure;

public sealed record TerminalFrame(
    IRenderable Header,
    IReadOnlyList<IRenderable> Content,
    StatusBlock Status);
