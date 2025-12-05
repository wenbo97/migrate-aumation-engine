using Automation.DotnetUpgradeWorkflow.Tools;
using Automation.KernelContainerProvider;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Automation.CliRunner;

/// <summary>
/// 
/// </summary>
internal class RunnerCLI
{
    /// <summary>
    /// 
    /// </summary>
    private static ILogger<RunnerCLI> Logger;

    public static async Task<int> Main(string[] args)
    {
        // Init logger factory.
        ILoggerFactory loggerFactory = LoggingBootstrapper.InitLogging();
        Logger = loggerFactory.CreateLogger<RunnerCLI>();
        
        Logger.LogInformation("Starting Automation Kernel CLI Runner.");

        // load .env file and create kernel.
        Kernel kernel = KernelBootstrapper.InitKernel(loggerFactory);
        kernel.Plugins.AddFromType<FileSystemTools>("FileSystem");
        // TrackTokenUsage tokenUsage = kernel.GetRequiredService<TrackTokenUsage>();

        return await PluginCommandBootstrapper.InitPluginCommandConfiguer(kernel: kernel, loggerFactory, args);
    }
}