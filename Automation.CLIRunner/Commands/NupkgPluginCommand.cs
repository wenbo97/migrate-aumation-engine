using System.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Services;
using AuthorRole = Microsoft.SemanticKernel.ChatCompletion.AuthorRole;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace Automation.CliRunner.Commands;

public class NupkgPluginCommand : IPluginCommand
{
    public string Name => "nupkg";

    public string PluginTaskWorkRoot => "nupkg-analyze";

    public string PluginMarkdownName => PluginTaskWorkRoot;

    // Logger.
    public ILogger Logger { get; set; }

    /// <summary>
    /// NupkgPlugin Generation.
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="loggerFactory"></param>
    /// <returns>Command.</returns>
    public Command BuildPluginCommand(Kernel kernel, ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger<NupkgPluginCommand>();

        Command nupkgCommand = new Command(Name, "NupkgPlugin use to analyze *.nupkg.(proj|csproj) files.");

        Command analyzeCommand = new Command("analyze",
            "Analyze Nupkg(*.nupkg.proj|*.nupkg.csproj) config file under a folder path.");

        var folderOption = new Option<DirectoryInfo>(
            name: "folderPath",
            aliases: ["--folder", "-f"]
        )
        {
            Description = "The folder to process Nupkg analysis.",
            DefaultValueFactory = result => new DirectoryInfo(Directory.GetCurrentDirectory())
        };

        analyzeCommand.Add(folderOption);
        analyzeCommand.SetAction(async parseResult =>
            await this.RunAnalyzePlugin(kernel, parseResult.GetValue(folderOption)));

        nupkgCommand.Subcommands.Add(analyzeCommand);
        return nupkgCommand;
    }

    /// <summary>
    /// Run Nupkg analysis scenario task.
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="folder"></param>
    /// <returns>Task.</returns>
    private async Task<int> RunAnalyzePlugin(Kernel kernel, DirectoryInfo? folder)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        ArgumentNullException.ThrowIfNull(folder);

        string nupkgMdFile = Path.Combine(Directory.GetCurrentDirectory(), IPluginCommand.TaskMarkdownTopRootPath,
            PluginTaskWorkRoot, "nupkg-analyze.md");

        if (!File.Exists(nupkgMdFile))
        {
            this.Logger.LogError($"Cannot find {nupkgMdFile}. file.");
            return 0;
        }

        string nupkgMdContent = await File.ReadAllTextAsync(nupkgMdFile);


        this.Logger.LogInformation("Exc task markdown: {MarkdownFile} by AI. Markdown task content:\n{MarkdownContent}",
            PluginMarkdownName, nupkgMdContent);

        PromptExecutionSettings executionSettings = new OpenAIPromptExecutionSettings()
        {
            MaxTokens = 4096,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };
        // Execute md task file by chat completion service.
        IChatCompletionService completionService =
            kernel.GetRequiredService<IChatCompletionService>();
        
        ChatHistory history = new ChatHistory();
        history.AddSystemMessage("You are an assistant for NuGet package project analysis. Please strictly follow the user's Markdown instructions.");
        history.AddSystemMessage("Do not provide any next steps consideration. Once the task based on the markdown is finished, provide a summary.");
        history.AddUserMessage(nupkgMdContent);
        var resultContent = await completionService.GetChatMessageContentsAsync(history, executionSettings, kernel);
        string? modelId= completionService.GetModelId();
        Logger.LogInformation("Model ID: {ModelId}", modelId);
        
        foreach (var messageContent in resultContent)
        {
            // if (messageContent.Metadata is not null)
            // {
            //     foreach (var keyValuePair in messageContent.Metadata)
            //     {
            //         this.Logger.LogInformation("Response metadata: {Key} - {Value}", keyValuePair.Key, keyValuePair.Value);
            //     }
            // }
            this.Logger.LogInformation("AI role: {Role}", messageContent.Role);
            this.Logger.LogInformation("AI content:\n{Content}", messageContent.ToString());
            
            history.AddAssistantMessage(messageContent.ToString());
        }

        this.Logger.LogInformation("=== Task End ===");
        return IPluginCommand.SucceededCode;
    }
}