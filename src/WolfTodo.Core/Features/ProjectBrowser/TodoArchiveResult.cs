namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoArchiveResult(
    bool Succeeded,
    int ArchivedCount,
    string ArchivePath,
    string? Error)
{
    public static TodoArchiveResult Success(string archivePath, int archivedCount) =>
        new(true, archivedCount, archivePath, null);

    public static TodoArchiveResult Failure(string archivePath, int archivedCount, string error) =>
        new(false, archivedCount, archivePath, error);
}
