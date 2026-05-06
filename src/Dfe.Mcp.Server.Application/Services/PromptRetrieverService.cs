using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Enums;
using Dfe.Mcp.Server.Application.Extensions;
using Dfe.Mcp.Server.Application.FileRetrievers.Interfaces;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dfe.Mcp.Server.Application.Services;

public class PromptRetrieverService(IPromptFileReader fileReader, PromptConfiguration promptConfiguration, ILogger<PromptRetrieverService> logger) : IPromptRetrieverService
{
    public string GetSystemPrompt(SystemPromptType promptType) =>
        GetPrompt(promptConfiguration.SystemPrompts, promptType, GetSystemEmbeddedFallback);

    public string GetUserPrompt(UserPromptType promptType) =>
        GetPrompt(promptConfiguration.UserPrompts, promptType, GetUserEmbeddedFallback);

    private string GetPrompt<TPromptType>(IDictionary<TPromptType, string> prompts, TPromptType promptType, Func<TPromptType, string> fallback) where TPromptType : notnull
    {
        if (!prompts.TryGetValue(promptType, out var path))
        {
            logger.LogWarning("No prompt configured for type: {PromptType}", promptType);
            return fallback(promptType);
        }

        try
        {
            return fileReader.Read(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load prompt file for {PromptType}. Using fallback.", promptType);
            return fallback(promptType);
        }
    }

    private static string GetSystemEmbeddedFallback(SystemPromptType type) => type switch
    {
        SystemPromptType.BriefingTool => SystemPromptType.BriefingTool.GetDescription(),
        _ => "You are a general-purpose AI assistant."
    };

    private static string GetUserEmbeddedFallback(UserPromptType promptType) => promptType switch
    {
        UserPromptType.Ofsted => UserPromptType.Ofsted.GetDescription(),
        UserPromptType.OfstedSummary => UserPromptType.OfstedSummary.GetDescription(),
        UserPromptType.OverallSummary => UserPromptType.OverallSummary.GetDescription(),
        UserPromptType.Concerns => UserPromptType.Concerns.GetDescription(),
        _ => "You are a general-purpose user prompt."
    };
}
