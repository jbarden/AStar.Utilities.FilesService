namespace AStar.Utilities.FilesService.Models;

public class ConfigurationSettings
{
    public static string SettingName => "ConfigurationSettings";

    public string[] Directories { get; set; } = Array.Empty<string>();

    public string FileSaveDirectory { get; set; } = string.Empty;

    public string FileSaveName { get; set; } = string.Empty;

    public string FileSaveDirectoryAndName => @$"{FileSaveDirectory}\{FileSaveName}";

    public string EmptyFilesSaveDirectoryAndName => @$"{FileSaveDirectory}\{EmptyFilesSaveName}";

    public string EmptyFilesSaveName { get; set; } = string.Empty;
}
