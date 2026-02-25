using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using OpenAI;
using WoofAgent.Shared.Configuration;

namespace WoofAgent.Providers;

public static class KernelBuilderExtensions
{
    public static IKernelBuilder AddLlmProvider(this IKernelBuilder builder, LlmSettings settings)
    {
        return settings.DefaultProvider.ToLowerInvariant() switch
        {
            "openai" => builder.AddOpenAiProvider(settings.OpenAi),
            "gemini" => builder.AddGeminiProvider(settings.Gemini),
            _ => throw new ArgumentException($"Unknown provider: {settings.DefaultProvider}")
        };
    }

    private static IKernelBuilder AddOpenAiProvider(this IKernelBuilder builder, OpenAiSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("OpenAI API key is not configured. Set it via user-secrets.");

        if (!string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            // Custom endpoint (OpenRouter, Azure, local, etc.)
            var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) };
            var client = new OpenAIClient(new System.ClientModel.ApiKeyCredential(settings.ApiKey), clientOptions);
            builder.AddOpenAIChatCompletion(settings.ModelId, client);
        }
        else
        {
            // Standard OpenAI
            builder.AddOpenAIChatCompletion(
                modelId: settings.ModelId,
                apiKey: settings.ApiKey);
        }

        return builder;
    }

    private static IKernelBuilder AddGeminiProvider(this IKernelBuilder builder, GeminiSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("Gemini API key is not configured. Set it via user-secrets.");

#pragma warning disable SKEXP0070 // Google connector is experimental
        builder.AddGoogleAIGeminiChatCompletion(
            modelId: settings.ModelId,
            apiKey: settings.ApiKey);
#pragma warning restore SKEXP0070

        return builder;
    }
}
