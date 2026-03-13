using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace WoofAgent.Core.Agents;

/// <summary>
/// Smart washer agent that demonstrates native tool chaining via IAutoFunctionInvocationFilter.
/// The chaining logic (schedule_washing -> check_detergent) is handled by WasherChainingFilter,
/// not by LLM prompt instructions — making it reliable regardless of LLM capability.
/// </summary>
public class SmartWasherAgent : IAgent
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatService;
    private readonly ChatHistory _chatHistory;

    private const string SystemPrompt = """
        You are a smart washer assistant for a connected home.
        You help users manage their washing machine and related supplies.

        You have access to the following tools:
        - schedule_washing: Schedule a washing cycle at a given time
        - check_detergent: Check the current detergent level in the washer
        - add_to_cart: Add items to the user's online shopping cart

        When you schedule a wash, the detergent level is automatically checked for you.
        If the detergent is low, ask the user if they'd like to add detergent to their shopping cart.
        Do NOT call add_to_cart unless the user explicitly confirms.
        Be concise and helpful.
        """;

    public string Name => "SmartWasher";

    public SmartWasherAgent(Kernel kernel)
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

    public void ClearHistory()
    {
        _chatHistory.Clear();
        _chatHistory.AddSystemMessage(SystemPrompt);
    }
}
