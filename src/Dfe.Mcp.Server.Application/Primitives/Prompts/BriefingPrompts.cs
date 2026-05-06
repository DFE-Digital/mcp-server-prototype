using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Enums;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Primitives.Prompts;

[McpServerPromptType]
public class BriefingPrompts(IPromptRetrieverService promptRetrieverService)
{
    [McpServerPrompt(Name = "BriefingSystemInstructionPrompt", Title = "Briefing system instruction prompt"), Description("Generates a briefing system instruction prompt.")]
    [Authorize(Policy = McpRoles.ReadPrompts)]
    public string BriefingSystemInstructionPrompt()
    {
        var prompt = promptRetrieverService.GetPrompt(PromptType.SystemInstruction);

        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("System instruction prompt is missing or empty.");

        return prompt;
    }
    [McpServerPrompt(Name = "OfstedPrompt", Title = "Ofsted prompt"), Description("Generates an Ofsted prompt.")]
    [Authorize(Policy = McpRoles.BriefingTool)]
    public string OfstedPrompt()
    {
        var prompt = promptRetrieverService.GetPrompt(PromptType.Ofsted);

        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("Ofsted prompt is missing or empty.");

        return prompt;
    }
}
