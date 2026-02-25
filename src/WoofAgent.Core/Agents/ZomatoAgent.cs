using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace WoofAgent.Core.Agents;

/// <summary>
/// Food ordering agent that uses MCP tools from the Zomato server.
/// MCP tools are registered on the kernel externally — this agent just uses them via auto function calling.
/// </summary>
public class ZomatoAgent : IAgent
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ChatHistory _chatHistory;

    private const string SystemPrompt = """
        You are a helpful food ordering assistant powered by Zomato.
        You help users discover restaurants, browse menus, and place food orders.

        You have access to Zomato tools that let you:
        - Search for restaurants by cuisine, location, or name
        - Browse restaurant menus and prices
        - Place and track food orders
        - Check delivery estimates and restaurant ratings

        Be friendly and concise. Help users find what they're craving.
        When suggesting restaurants, mention ratings and estimated delivery time when available.
        Confirm order details with the user before placing an order.
        """;

    public string Name => "Zomato";

    public ZomatoAgent(Kernel kernel)
    {
        _kernel = kernel;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _chatHistory = new ChatHistory(SystemPrompt);
    }

    public async Task<string> InvokeAsync(string prompt, CancellationToken cancellationToken = default)
    {
        _chatHistory.AddUserMessage(prompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await _chatService.GetChatMessageContentAsync(
            _chatHistory,
            settings,
            _kernel,
            cancellationToken);

        _chatHistory.AddAssistantMessage(response.Content ?? string.Empty);

        return response.Content ?? string.Empty;
    }
}
