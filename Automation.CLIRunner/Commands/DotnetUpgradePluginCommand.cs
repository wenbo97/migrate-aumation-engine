using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Automation.DotnetUpgradeWorkflow;
using DotNetEnv;
using OpenAI;

namespace Automation.CliRunner.Commands;

/**
 * DotnetUpgradePluginCommand
 */
public class DotnetUpgradePluginCommand : IPluginCommand
{
    public string Name => "dotnet-upgrade-workflow";
    public string PluginTaskWorkRoot => "c:/src/ControlPlane";
    public string PluginMarkdownName => PluginTaskWorkRoot;
    public ILogger Logger { get; set; }

    public Command BuildPluginCommand(Kernel kernel, ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<DotnetUpgradePluginCommand>();

        Command dotnetUpgradeCommand = new Command(Name, "Csproj upgrade automation.");

        Command startCommand = new Command("start", "Plan and execute csproj migration with a folder path.");

        var folderOption = new Option<DirectoryInfo>(
            name: "csprojFolderPath",
            aliases: ["--csprojPath", "-cp"]
        )
        {
            Description = "The folder containing the csproj file to process.",
            Required = true
        };

        startCommand.Add(folderOption);

        startCommand.SetAction(async parseResult =>
        {
            var directoryInfo = parseResult.GetValue(folderOption);
            if (directoryInfo == null || !directoryInfo.Exists)
            {
                Logger.LogError("Invalid directory path provided.");
                return;
            }

            try
            {
                await this.RunAnalyzePlugin(kernel, directoryInfo.FullName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fatal error during migration workflow.");
            }
        });

        dotnetUpgradeCommand.Subcommands.Add(startCommand);
        return dotnetUpgradeCommand;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="projectPath"></param>
    /// <exception cref="ArgumentNullException"></exception>
    private async Task RunAnalyzePlugin(Kernel kernel, string projectPath)
    {
        Logger.LogInformation("Starting Automation Workflow for: {ProjectPath}", projectPath);


        // 1. Load Environment Variables
        var envConfig = Env.Load(".env").ToDictionary(k => k.Key, v => v.Value);

        string apiKey = string.Empty;
        string modelId = string.Empty;
        string openAiEndpoint = string.Empty;
        apiKey = envConfig["OPEN_API_KEY"] ?? throw new ArgumentNullException(nameof(apiKey));
        modelId = envConfig["MODEL_NAME"] ?? throw new ArgumentNullException(nameof(modelId));
       
        // 3. Initialize OpenAI Client (Shared connection)
        var openAIClient = new OpenAIClient(apiKey);
       
        // 4. Instantiate Orchestrator
        var orchestrator = new MigrationOrchestrator(Logger, kernel, openAIClient, modelId);

        // 5. Run the workflow
        await orchestrator.RunAsync(projectPath);

        Logger.LogInformation("Automation Workflow Completed.");
    }
}