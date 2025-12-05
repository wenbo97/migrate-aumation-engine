using System.ComponentModel;
using System.ClientModel;
using System.Text.Json;
using DotNetEnv;
using Example.AgentFramework.Usage.customs;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

namespace Example.AgentFramework.Usage;

public class Program
{
    // 🎲 Tool Function: Random Destination Generator
// This static method will be available to the agent as a callable tool
// The [Description] attribute helps the AI understand when to use this function
// This demonstrates how to create custom tools for AI agents
    [Description("Provides a random vacation destination.")]
    public static string GetRandomDestination()
    {
        // List of popular vacation destinations around the world
        // The agent will randomly select from these options
        Console.WriteLine("Call GetRandomDestination");
        var destinations = new List<string>
        {
            "Paris, France", // European cultural capital
            "Tokyo, Japan", // Asian modern metropolis
            "New York City, USA", // American urban experience
            "Sydney, Australia", // Oceanic coastal beauty
            "Rome, Italy", // Historical European city
            "Barcelona, Spain", // Mediterranean cultural hub
            "Cape Town, South Africa", // African scenic destination
            "Rio de Janeiro, Brazil", // South American beach city
            "Bangkok, Thailand", // Southeast Asian cultural center
            "Vancouver, Canada" // North American natural beauty
        };

        // Generate random index and return selected destination
        // Uses System.Random for simple random selection
        var random = new Random();
        int index = random.Next(destinations.Count);
        return destinations[index];
    }

    public static async Task TryExmaple1(OpenAIClient openAiClient, string modelId)
    {
        // 🤖 Create AI Agent with Travel Planning Capabilities
        // Initialize OpenAI client, get chat client for specified model, and create AI agent
        // Configure agent with travel planning instructions and random destination tool
        // The agent can now plan trips using the GetRandomDestination function
        const string travelAgentName = "TravelAgent";

        const string instruction = @"""
You are a helpful AI Agent that can help plan vacations for customers.

Important: When users specify a destination, always plan for that location. Only suggest random destinations when the user hasn't specified a preference.

When the conversation begins, introduce yourself with this message:
'Hello! I'm your TravelAgent assistant. I can help plan vacations and suggest interesting destinations for you. Here are some things you can ask me:
1. Plan a day trip to a specific location
2. Suggest a random vacation destination
3. Find destinations with specific features (beaches, mountains, historical sites, etc.)
4. Plan an alternative trip if you don't like my first suggestion

What kind of trip would you like me to help you plan today?'

Always prioritize user preferences. If they mention a specific destination like 'Bali' or 'Paris,' focus your planning on that location rather than suggesting alternatives.

""";

        AIAgent agent = openAiClient
            .GetChatClient(modelId)
            .CreateAIAgent(
                name: travelAgentName,
                instructions: instruction,
                tools: [AIFunctionFactory.Create((Func<string>)GetRandomDestination)] // Register the tool function
            );

        AgentThread thread = agent.GetNewThread();
        Console.WriteLine(await agent.RunAsync("Plan me a day trip in Bali", thread));
    }

    public static async Task TryOpenAITools(OpenAIClient openAiClient, string modelId)
    {
        // 🤖 Create AI Agent with Travel Planning Capabilities
        // Initialize OpenAI client, get chat client for specified model, and create AI agent
        // Configure agent with travel planning instructions and random destination tool
        // The agent can now plan trips using the GetRandomDestination function
        const string travelAgentName = "ListToolAgent";

        const string instruction = @"""List the supported tools.""";

        ChatClientAgentOptions agentOptions = new ChatClientAgentOptions(
            name: travelAgentName,
            instructions: instruction,
            description: "Travel plan",
            tools: [AIFunctionFactory.Create((Func<string>)GetRandomDestination)]
        );

        AIAgent agent = openAiClient
            .GetChatClient(modelId)
            .CreateAIAgent(agentOptions);

        AgentThread thread = agent.GetNewThread();
        Console.WriteLine(await agent.RunAsync("Plan me a day trip in Bali", thread));
    }
    
    
   
    


    public static async Task Main(string[] args)
    {
        Env.Load(".env");
        var github_endpoint = Environment.GetEnvironmentVariable("GITHUB_ENDPOINT") ?? throw new InvalidOperationException("GITHUB_ENDPOINT is not set.");
        var github_model_id = Environment.GetEnvironmentVariable("GITHUB_MODEL_ID") ?? "gpt-4o-mini";
        var github_token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN is not set.");


        var openAIOptions = new OpenAIClientOptions()
        {
            Endpoint = new Uri(github_endpoint)
        };

        var openAIClient = new OpenAIClient(new ApiKeyCredential(github_token), openAIOptions);

        // await TryExmaple1(openAIClient, github_model_id);
        // await TryOpenAITools(openAIClient, github_model_id);
        // await TryOpenAITools(openAIClient, github_model_id);
        // await TryWorkflow(openAIClient, github_model_id);

        await ExampleWorkflowUsage.TryWorkflow2();
        await ExampleWorkflowUsage.TryWorkflowWithAgents();
    }
    
}


