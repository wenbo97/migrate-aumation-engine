using Automation.DotnetUpgradeWorkflow.Tools;
using Automation.Plugin.NupkgAnalyzerPlugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using Newtonsoft.Json;
using OpenAI;
using Serilog;

namespace Automation.KernelContainerProvider;

public class KernelProvider : IKernelProvider
{
    private readonly ILogger<KernelProvider> logger;

    private readonly ILoggerFactory loggerFactory;

    private KernelOptions options;

    private Kernel kernel { get; init; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options">KernelOptions.</param>
    /// <param name="loggerFactory">loggerFactory.</param>
    public KernelProvider(KernelOptions options, ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        this.logger = loggerFactory.CreateLogger<KernelProvider>();

        this.kernel = this.CreateKernelInternalAsync();
    }

    private Kernel CreateKernelInternalAsync()
    {
        this.logger.LogInformation("Creating Semantic Kernel. Backend: {Backend}, Model: {Model}",
            this.options.Backend, this.options.ModelId);

        var builder = Kernel.CreateBuilder();


        switch (this.options.Backend)
        {
            case KernelBackend.OpenAI:
            {
                builder.Services.AddSingleton(this.loggerFactory);
                builder.Services.AddSingleton<TrackTokenUsage>();
                builder.Services.AddScoped<IFunctionInvocationFilter, TokenUsageFilter>();
                var httpClient = new HttpClient()
                {
                    Timeout = TimeSpan.FromSeconds(800)
                };
                builder.AddOpenAIChatCompletion(modelId: this.options.ModelId, apiKey: this.options.ApiKey,
                    httpClient: httpClient);

                break;
            }

            case KernelBackend.AzureOpenAI:
            {
                throw new NotImplementedException("Only support OpenAI model for now.");
            }

            default:
                throw new NotSupportedException($"Unsupported backend: {this.options.Backend}");
        }

        // Add plugin class kernel functions from class type.
        builder.Plugins.AddFromType<NupkgAnalyzerPlugin>();

        // Build kernel.
        Kernel kernelInstance = builder.Build();

        // Add semantic function and import to its plugin.
        string semanticFunctionRootDir = Path.Combine(Directory.GetCurrentDirectory(), "SemanticFunctions",
            nameof(NupkgAnalyzerPlugin));
        kernelInstance.ImportPluginFromPromptDirectory(pluginDirectory: semanticFunctionRootDir,
            pluginName: "NupkgAnalyzerPluginSemanticFunctions");

        this.logger.LogInformation("Semantic Kernel created successfully. Registered Plugins: ");
        foreach (var plugin in kernelInstance.Plugins)
        {
            this.logger.LogInformation("Plugin: {PluginName}", plugin.Name);

            foreach (var func in plugin.GetFunctionsMetadata())
            {
                this.logger.LogInformation("Function: {FunctionName}, Parameters:{Parameters}", func.Name,
                    JsonConvert.SerializeObject(func.Parameters));
                this.logger.LogInformation("  - Description: {Description}", func.Description);
            }
        }

        this.logger.LogInformation("Semantic Kernel has created successfully.");
        return kernelInstance;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public Kernel GetKernel()
    {
        return this.kernel ?? throw new NullReferenceException(nameof(this.kernel));
    }
}