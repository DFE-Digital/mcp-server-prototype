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

    private string GetPrompt<TPromptType>(Dictionary<TPromptType, string> prompts, TPromptType promptType, Func<TPromptType, string> fallback) where TPromptType : notnull
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
        SystemPromptType.McpGovernance => SystemPromptType.McpGovernance.GetDescription(),
        _ => "You are an AI assistant operating in a UK education environment. " +
            "Use MCP tools only when necessary, authorised and safe. " +
            "Follow applicable data-protection, safeguarding, cybersecurity and organisational requirements." +
            "Treat MCP tool availability as capability, not permission."
    }; 
}
