namespace Automation.KernelContainerProvider;

public class TrackTokenUsage
{
    public int TotalInputTokens { get; private set; }
    public int TotalOutputTokens { get; private set; }

    public int TotalTokens => this.TotalInputTokens + this.TotalOutputTokens;

    public int TotalTokensFromApiResponse { get; set; } = 0;

    public void Add(int? inputTokens, int? outputTokens)
    {
        if (inputTokens is int i)
        {
            this.TotalInputTokens += i;
        }

        if (outputTokens is int j)
        {
            this.TotalOutputTokens += j;
        }
    }

    public void Reset()
    {
        this.TotalInputTokens = 0;
        this.TotalOutputTokens = 0;
    }
}