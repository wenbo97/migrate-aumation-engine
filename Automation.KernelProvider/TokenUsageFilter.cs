using Json.More;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Automation.KernelContainerProvider;

/// <summary>
/// Token usage.
/// </summary>
public class TokenUsageFilter : IFunctionInvocationFilter
{
    private readonly ILogger<TokenUsageFilter> logger;

    private readonly TrackTokenUsage tokenUsage;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="tokenUsage"></param>
    public TokenUsageFilter(ILoggerFactory loggerFactory, TrackTokenUsage tokenUsage)
    {
        this.logger = loggerFactory.CreateLogger<TokenUsageFilter>();
        this.tokenUsage = tokenUsage;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        string functionName = context.Function.Name;
        string pluginName = context.Function.PluginName ?? "UnknownPlugin";
        this.logger.LogInformation("Function: [{FunctioName}] is called in Plugin: [{PluginName}]", functionName,
            pluginName);

        string functionArgsJsonResponse = context.Arguments.ToJsonDocument()?.RootElement.ToJsonString() ?? string.Empty;
        this.logger.LogInformation("Argument: [{FunctionArg}]", functionArgsJsonResponse);
        
        await next(context);
        
        var resultMeta = context.Result.Metadata ?? null;

        if (resultMeta is null || !resultMeta.ContainsKey("Usage"))
        {
            this.logger.LogWarning(
                "This kernel function result does not have metadata directory or no 'Usage' key in response.");
            return;
        }
        
        var usageElement = resultMeta["Usage"].ToJsonDocument()?.RootElement;
        if (usageElement is null)
        {
            this.logger.LogWarning("Usage metadata exists but cannot be converted to JsonDocument.");
            return;
        }
        
        var usageJsonResponse = usageElement.Value.ToJsonString();
        this.logger.LogInformation("Token Usage raw json: [{TokenUsage}]", usageJsonResponse);

        int? inputTokens = null;
        int? outputTokens = null;

        if (usageElement.Value.TryGetProperty("InputTokenCount", out var inProp))
        {
            inputTokens = inProp.GetInt32();
        }

        if (usageElement.Value.TryGetProperty("OutputTokenCount", out var outProp))
        {
            outputTokens = outProp.GetInt32();
        }

        this.tokenUsage.Add(inputTokens, outputTokens);
        // this.tokenUsage.TotalTokensFromApiResponse += 
            
        this.logger.LogInformation(
            "Token usage this call: in={Input}, out={Output}. Accumulated total: {Total}",
            inputTokens, outputTokens, this.tokenUsage.TotalTokens);
    }
}