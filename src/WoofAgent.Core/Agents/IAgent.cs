namespace WoofAgent.Core.Agents;

public interface IAgent
{
    string Name { get; }
    Task<string> InvokeAsync(string prompt, CancellationToken cancellationToken = default);
}
