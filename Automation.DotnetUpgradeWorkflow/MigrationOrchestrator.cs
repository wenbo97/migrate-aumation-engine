using System.ClientModel;
using Automation.DotnetUpgradeWorkflow.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenAI;
using OpenAI.Chat;

namespace Automation.DotnetUpgradeWorkflow;


public class MigrationOrchestrator
{
    private readonly ILogger _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly string _modelId;
    private readonly Kernel _kernel;

    public MigrationOrchestrator(
        ILogger logger,
        Kernel kernel,
        OpenAIClient openAIClient,
        string modelId)
    {
        this._kernel = kernel;
        this._logger = logger;
        this._openAIClient = openAIClient;
        this._modelId = modelId;
    }

    public async Task RunAsync(string projectPath)
    {
        // 1. Create Agents

        // share and use the same shell.
        using PersistentShell shell = await this.InitDevSession();
        
        var builderAgent = await this.CreateBuilderAgent(shell);
        
        // Invoke the agent with streaming support.
        AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates = builderAgent.RunStreamingAsync([$"CD to {projectPath} path and build this project"]);
        await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
        {
            if (completionUpdate.ContentUpdate.Count > 0)
            {
                Console.Write(completionUpdate.ContentUpdate[0].Text);
            }
        }

        this._logger.LogInformation("Build agent finished.");
        
        await Task.FromResult("done.");
    }

    private async Task<PersistentShell> InitDevSession()
    {
        var buildEnvVars = new Dictionary<string, string>
        {
            { "MySdk_Target_NetVersion", "net8.0" },
            { "UseSharedCompilation", "false" },
            { "POWERSHELL_CLI_TELEMETRY_OPTOUT", "1" },
            { "POSH_CLI_PROGRESS_PREFERENCE", "SilentlyContinue" },
            // Inherit PATH to ensure dotnet CLI is found
            { "PATH", Environment.GetEnvironmentVariable("PATH") ?? "" }
        };

        var shell = new PersistentShell(this._logger, buildEnvVars);

        // Start and Configure the Shell to reuse in workflow
        shell.Start();
        TimeSpan defaultTimeout = TimeSpan.FromMinutes(5);

        Console.WriteLine(">>> Initializing Shell Environment...");
        await shell.ExecuteCommandAsync(@".\tools\path1st\myenv.cmd", defaultTimeout);
        await shell.ExecuteCommandAsync($"DIR", defaultTimeout);

        return shell;
    }
    
    /// <summary>
    ///  Build Agent.
    /// </summary>
    /// <param name="shell">An active ControlPlane dev shell</param>
    /// <returns></returns>
  private async Task<AIAgent> CreateBuilderAgent(PersistentShell shell)
    {
        var tools = new List<AITool>();

        var filePlugin = KernelPluginFactory.CreateFromType<FileSystemTools>("FileSystem");
        foreach (var func in filePlugin.AsAIFunctions(this._kernel))
        {
            tools.Add(func);
        }
        
        var shellTool = new ShellPlugin(shell);
        var commandTool = AIFunctionFactory.Create(
            shellTool.ExecuteCommandAsync, 
            name: "ExecuteCommand", 
            description: "Executes a Windows CMD command in the configured build environment."
        );
        tools.Add(commandTool);
        
        // 3. Create the Agent
         return this._openAIClient.GetChatClient(_modelId).CreateAIAgent(
             name: "Builder",
          instructions: """
                           You are a dev environment Build Engineer specialized in .NET automation.
                           Your goal is to facilitate the repository upgrade from net472 to supporting both net472 and net8.0 by executing builds.

                           ### CRITICAL EXECUTION RULE
                           - The shell environment **DOES NOT** persist directory changes between command calls.
                           - You **MUST** combine the directory navigation (`cd`) and the build command into a **SINGLE** command line using `&&`.
                           - **Incorrect**: Calling `ExecuteCommand("cd path")` then `ExecuteCommand("build")`.
                           - **Correct**: Calling `ExecuteCommand("cd /d \"path\\to\\project\" && build")`.

                           ### Build Command Definitions
                           - **build**: Construct the command as `cd /d "<Project_Directory>" && build`. (Do not add flags to 'build')
                           - **quickbuild**: Construct the command as `cd /d "<Project_Directory>" && quickbuild -notest`.

                           ### Protocol
                           1. **Analyze Context**:
                              - Identify the directory containing the target `.csproj` file from the request.
                              - You may optionally run `dir "<Project_Directory>"` first to assess project size, but do not rely on it setting the current directory.

                           2. **Determine Timeout**:
                              - Based on project size estimation:
                                - **Small** (< 10MB): "00:05:00"
                                - **Medium** (10MB - 100MB): "00:15:00"
                                - **Large** (> 100MB): "00:30:00"

                           3. **Execute Build**:
                              - Use the 'ExecuteCommand' tool.
                              - **ACTION**: Execute the **chained command** (navigate AND build) in one go.
                              - Example: `cd /d "C:\Repo\MyProject" && build`
                              - Pass the calculated timeout string.

                           4. **Verify & Analyze**:
                              - Return the stdout/stderr captured by the tool.
                              - If the build fails, analyze the output. Do not attempt to fix environment variables.
                           """,
             tools: tools
         );
    }
}