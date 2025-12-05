using Microsoft.Agents.AI.Workflows;

namespace Example.AgentFramework.Usage;

public sealed class ExampleWorkflowUsage
{
    public static async Task TryWorkflow2()
    {
        // Create the executors
        Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
        var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

        ReverseTextExecutor reverse = new();

        // Build the workflow by connecting executors sequentially
        WorkflowBuilder builder = new(uppercase);
        builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
        var workflow = builder.Build();

        // Execute the workflow with input data
        await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");
        foreach (WorkflowEvent evt in run.NewEvents)
        {
            if (evt is ExecutorCompletedEvent executorComplete)
            {
                Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
            }
        }
    }

    public static async Task TryWorkflowWithAgents()
    {
        throw new NotImplementedException();
    }
}

internal sealed class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
{
    public override ValueTask<string> HandleAsync(string input, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // Reverse the input text
        return ValueTask.FromResult(new string(input.Reverse().ToArray()));
    }
}