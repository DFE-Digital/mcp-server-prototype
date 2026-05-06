using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Enums;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Primitives.Prompts;

[McpServerPromptType]
public class Prompts(IPromptRetrieverService promptRetrieverService)
{
    [McpServerPrompt(Name = "GetSystemPrompt", Title = "Gets system instruction prompt"), Description("Gets a system instruction prompt.")]
    [Authorize(Policy = McpRoles.ReadPrompts)]
    public string GetSystemPrompt(SystemPromptType promptType)
    {
        var prompt = promptRetrieverService.GetSystemPrompt(promptType);

        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("System instruction prompt is missing or empty.");

        return prompt;
    }
    [McpServerPrompt(Name = "GetUserPrompt", Title = "Gets user prompt"), Description("Gets the user prompt.")]
    [Authorize(Policy = McpRoles.BriefingTool)]
    public string GetUserPrompt(UserPromptType promptType)
    {
        var prompt = promptRetrieverService.GetUserPrompt(promptType);

        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("Ofsted prompt is missing or empty.");

        return prompt;
    }
}
