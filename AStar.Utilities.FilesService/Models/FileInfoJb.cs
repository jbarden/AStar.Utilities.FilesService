namespace AStar.Utilities.FilesService.Models;

internal class FileInfoJb
{
    public string DirectoryName { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime CreationTimeUtc { get; set; }

    public DateTime LastAccessTimeUtc { get; set; }

    public DateTime LastWriteTimeUtc { get; set; }

    public string FullName => Path.Combine(DirectoryName, FileName);
}
