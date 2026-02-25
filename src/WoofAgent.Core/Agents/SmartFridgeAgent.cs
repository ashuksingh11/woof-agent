using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace WoofAgent.Core.Agents;

/// <summary>
/// Smart fridge assistant agent that can call fridge plugin functions automatically.
/// </summary>
public class SmartFridgeAgent : IAgent
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ChatHistory _chatHistory;

    private const string SystemPrompt = """
        You are a helpful smart fridge assistant on a Samsung Family Hub (Tizen).
        You help families manage their kitchen, food inventory, schedules, and daily tasks.

        You have access to the following capabilities:
        - View and manage fridge inventory
        - Check calendar events for the family
        - Suggest recipes based on available ingredients
        - Check the weather
        - Set kitchen timers
        - Manage the shopping list
        - Check fridge status and alerts

        Be friendly, concise, and proactive. If you notice items expiring soon, mention it.
        When suggesting recipes, consider what's about to expire.
        Keep responses suitable for display on a fridge screen (not too long).
        """;

    public string Name => "SmartFridge";

    public SmartFridgeAgent(Kernel kernel)
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

    /// <summary>
    /// Clears the conversation history, keeping only the system prompt.
    /// </summary>
    public void ClearHistory()
    {
        _chatHistory.Clear();
        _chatHistory.AddSystemMessage(SystemPrompt);
    }
}
