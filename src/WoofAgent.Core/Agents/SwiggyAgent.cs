using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace WoofAgent.Core.Agents;

/// <summary>
/// Food ordering agent that uses MCP tools from a Swiggy server (Food, Instamart, or Dineout).
/// MCP tools are registered on the kernel externally — this agent just uses them via auto function calling.
/// </summary>
public class SwiggyAgent : IAgent
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ChatHistory _chatHistory;

    public string Name { get; }

    private static string GetSystemPrompt(SwiggyService service) => service switch
    {
        SwiggyService.Food => """
            You are a helpful food ordering assistant powered by Swiggy.
            You help users discover restaurants, browse menus, and place food delivery orders.

            Be friendly and concise. Help users find what they're craving.
            When suggesting restaurants, mention ratings and estimated delivery time when available.
            Confirm order details with the user before placing an order.
            """,
        SwiggyService.Instamart => """
            You are a helpful grocery and essentials assistant powered by Swiggy Instamart.
            You help users find and order groceries, household essentials, and daily needs
            with quick delivery.

            Be friendly and concise. Help users find products efficiently.
            Mention availability and delivery estimates when possible.
            Confirm order details with the user before placing an order.
            """,
        SwiggyService.Dineout => """
            You are a helpful restaurant dining assistant powered by Swiggy Dineout.
            You help users discover restaurants for dine-in, book tables, and find deals
            for eating out.

            Be friendly and concise. Help users find great places to eat.
            Mention ratings, offers, and cuisine types when available.
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    public SwiggyAgent(Kernel kernel, SwiggyService service)
    {
        _kernel = kernel;
        Name = $"Swiggy{service}";
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _chatHistory = new ChatHistory(GetSystemPrompt(service));
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

public enum SwiggyService
{
    Food,
    Instamart,
    Dineout
}
