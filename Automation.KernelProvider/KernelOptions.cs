namespace Automation.KernelContainerProvider;

public sealed class KernelOptions
{
    public KernelBackend Backend { get; set; } = KernelBackend.OpenAI;

    public string ApiKey { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public string? Endpoint { get; set; } = string.Empty;

    public bool EnableVerboseLogging { get; set; } = false;
}