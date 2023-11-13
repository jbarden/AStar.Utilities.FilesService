using System.IO.Abstractions;
using AStar.Utilities.FilesService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace AStar.Utilities.FilesService;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var host = ConfigureServices(args);
        var options = host.Services.GetRequiredService<IOptions<ConfigurationSettings>>();
        var logger = host.Services.GetRequiredService<ILogger<WorkerService>>();
        var fileSystem = host.Services.GetRequiredService<IFileSystem>();

        var workerService = new WorkerService(fileSystem, options, logger);

        await workerService.ExecuteAsync(CancellationToken.None);
    }

    private static IHost ConfigureServices(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                _ = services.Configure<ConfigurationSettings>(
                    context.Configuration.GetSection(ConfigurationSettings.SettingName));
                _ = services.AddScoped<IFileSystem, FileSystem>();
            })
            .UseSerilog((context, configuration) => { _ = configuration.ReadFrom.Configuration(context.Configuration); })
            .Build();
}
