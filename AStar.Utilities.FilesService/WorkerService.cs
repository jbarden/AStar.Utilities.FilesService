using System.Collections;
using System.IO.Abstractions;
using System.Text.Json;
using AStar.Utilities.FilesService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AStar.Utilities.FilesService;

public class WorkerService
{
    private readonly IFileSystem fileSystem;
    private readonly ConfigurationSettings configurationSettings;
    private readonly ILogger<WorkerService> logger;

    public WorkerService(IFileSystem fileSystem, IOptions<ConfigurationSettings> configurationSettings, ILogger<WorkerService> logger)
    {
        this.fileSystem = fileSystem;
        this.configurationSettings = configurationSettings.Value;
        this.logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Starting file list retrieval...");
            var files = GetTheCurrentListOfFiles();
            var emptyFiles = await RetrieveTheStoredListOfEmptyFiles(stoppingToken);
            logger.LogInformation("Completed the file list retrieval...");
            var storedFilesList = await RetrieveTheStoredListOfFiles(stoppingToken);

            if (storedFilesList.Count != files.Count)
            {
                await UpdateTheFileList(files, storedFilesList, emptyFiles, stoppingToken);
            }
            else
            {
                logger.LogInformation("File list count matches stored value so we are skipping updating the details...");
            }

            logger.LogInformation("Completed file list retrieval...Having a rest...");

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task<List<FileInfoJb>> RetrieveTheStoredListOfFiles(CancellationToken stoppingToken)
    {
        var storedFilesList = new List<FileInfoJb>();
        if (!fileSystem.File.Exists(configurationSettings.FileSaveDirectoryAndName))
        {
            return storedFilesList;
        }

        logger.LogInformation("Loading {fileSaveDirectoryAndName}...", configurationSettings.FileSaveDirectoryAndName);
        return JsonSerializer.Deserialize<List<FileInfoJb>>(
            await fileSystem.File.ReadAllTextAsync(configurationSettings.FileSaveDirectoryAndName, stoppingToken))!;
    }

    private async Task<List<FileInfoJb>> RetrieveTheStoredListOfEmptyFiles(CancellationToken stoppingToken)
    {
        var storedEmptyFilesList = new List<FileInfoJb>();
        if (!fileSystem.File.Exists(configurationSettings.EmptyFilesSaveDirectoryAndName))
        {
            return storedEmptyFilesList;
        }

        logger.LogInformation("Loading {fileSaveDirectoryAndName}...", configurationSettings.EmptyFilesSaveDirectoryAndName);

        var readAllTextAsync = await fileSystem.File.ReadAllTextAsync(configurationSettings.EmptyFilesSaveDirectoryAndName, stoppingToken);
        return JsonSerializer.Deserialize<List<FileInfoJb>>(readAllTextAsync)!;
    }

    private List<string> GetTheCurrentListOfFiles()
    {
        var files = new List<string>();
        foreach (var directory in configurationSettings.Directories.Where(fileSystem.Directory.Exists))
        {
            files.AddRange(fileSystem.Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories));
        }

        return files;
    }

    private async Task UpdateTheFileList(IReadOnlyCollection<string> files, ICollection storedFilesList,
        List<FileInfoJb> emptyFiles, CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "File list count of {currentCount} does not match stored file list count of {storedFileCount} in {storedFilesName} so we are updating the details...",
            files.Count, storedFilesList.Count, configurationSettings.FileSaveDirectoryAndName);
        var listOfFiles = MapTheFileList(files);

        logger.LogInformation("Saving the updated {storedFilesName} file...", configurationSettings.FileSaveDirectoryAndName);
        var emptyFilesCurrent = listOfFiles.Where(file => file.FileSize < 1000).ToList();
        emptyFilesCurrent.AddRange(emptyFiles);

        await SaveFileList(listOfFiles, configurationSettings.FileSaveDirectoryAndName, stoppingToken);
        await SaveFileList(emptyFilesCurrent, configurationSettings.EmptyFilesSaveName, stoppingToken);

        DeleteEmptyFiles(emptyFiles.ToArray(), stoppingToken);
    }

    private List<FileInfoJb> MapTheFileList(IEnumerable<string> files) => files.AsParallel().Select(fileSystem.FileInfo.New)
            .Select(fileInfo => new FileInfoJb
            {
                DirectoryName = fileInfo.DirectoryName!,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                CreationTimeUtc = fileInfo.CreationTimeUtc,
                LastAccessTimeUtc = fileInfo.LastAccessTimeUtc,
                LastWriteTimeUtc = fileInfo.LastWriteTimeUtc
            })
            .ToList();

    private async Task SaveFileList(List<FileInfoJb> listOfFiles, string fileNameWithDirectory, CancellationToken stoppingToken)
    {
        try
        {
            await fileSystem.File.WriteAllTextAsync(fileNameWithDirectory,
                JsonSerializer.Serialize(listOfFiles), stoppingToken);
        }
        catch (IOException)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));

            await fileSystem.File.WriteAllTextAsync(configurationSettings.FileSaveDirectoryAndName,
                JsonSerializer.Serialize(listOfFiles), stoppingToken);
        }
    }

    private void DeleteEmptyFiles(IEnumerable<FileInfoJb> emptyFiles, CancellationToken stoppingToken)
    {
        foreach (var fileName in emptyFiles.Select(file => file.FullName))
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            if (fileSystem.File.Exists(fileName))
            {
                fileSystem.File.Delete(fileName);
            }
        }
    }
}
