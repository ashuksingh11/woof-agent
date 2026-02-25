using Microsoft.SemanticKernel;

namespace WoofAgent.Core.Agents;

public class HelloWorldAgent : IAgent
{
    private readonly Kernel _kernel;

    public string Name => "HelloWorld";

    public HelloWorldAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> InvokeAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return result.GetValue<string>() ?? string.Empty;
    }
}
