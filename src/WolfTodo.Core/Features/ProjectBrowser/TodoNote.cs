namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoNote(int SourceLine, string Text, int LineCount = 1);
