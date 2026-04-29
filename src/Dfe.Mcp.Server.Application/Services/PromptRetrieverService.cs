using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Enums;
using Dfe.Mcp.Server.Application.Extensions;
using Dfe.Mcp.Server.Application.FileRetrievers.Interfaces;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dfe.Mcp.Server.Application.Services;

public class PromptRetrieverService(IPromptFileReader fileReader, PromptConfiguration promptConfiguration, ILogger<PromptRetrieverService> logger) : IPromptRetrieverService
{
    public string GetPrompt(PromptType type)
    { 
        if (!promptConfiguration.Files.TryGetValue(type, out var path))
        {
            logger.LogWarning("No prompt configured for type: {Type}", type);
            return GetEmbeddedFallback(type);
        } 

        try
        {
            return fileReader.Read(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load prompt file for {Type}. Using fallback.", type);
        }
         
        return GetEmbeddedFallback(type);
    }

    private static string GetEmbeddedFallback(PromptType type)
    {
        return type switch
        {
            PromptType.SystemInstruction => PromptType.SystemInstruction.GetDescription(), 
            PromptType.Ofsted => PromptType.Ofsted.GetDescription(), 
            PromptType.Concern => PromptType.Concern.GetDescription(), 
            _ =>
                "You are a general-purpose AI assistant."
        };
    }
}
