using Automation.KernelContainerProvider;
using DotNetEnv;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Automation.CliRunner;

public sealed class KernelBootstrapper
{
    private static ILogger<KernelBootstrapper> logger;
    
    public static Kernel InitKernel(ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<KernelBootstrapper>();
        
        var envConfig = Env.Load(".env").ToDictionary(k => k.Key, v => v.Value);

        string apiKey = envConfig["OPEN_API_KEY"] ?? throw new ArgumentNullException(nameof(apiKey));
        string modelId = envConfig["MODEL_NAME"] ?? throw new ArgumentNullException(nameof(modelId));
        string openAiEndpoint =
            envConfig["OPEN_AI_ENDPOINT"] ?? throw new ArgumentNullException(nameof(openAiEndpoint));


        KernelOptions options = new KernelOptions()
        {
            Backend = KernelBackend.OpenAI,
            ApiKey = apiKey,
            ModelId = modelId,
            EnableVerboseLogging = true,
            Endpoint = openAiEndpoint
        };

        IKernelProvider kernelProvider = new KernelProvider(options, loggerFactory);

        Kernel kernel = kernelProvider.GetKernel();
        logger.LogInformation("Kernel has generated.");

        return kernel;
    }
}