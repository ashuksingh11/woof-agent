namespace WoofAgent.Shared.Configuration;

public class LlmSettings
{
    public const string SectionName = "LlmProviders";

    public string DefaultProvider { get; set; } = "OpenAI";
    public OpenAiSettings OpenAi { get; set; } = new();
    public GeminiSettings Gemini { get; set; } = new();
}

public class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gpt-4o";
    public string? Endpoint { get; set; }  // Custom endpoint for OpenRouter, Azure, etc.
}

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gemini-2.0-flash";
}
