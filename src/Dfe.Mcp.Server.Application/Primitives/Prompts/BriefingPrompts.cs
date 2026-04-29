using Dfe.Mcp.Server.Application.Enums;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Primitives.Prompts;

[McpServerPromptType]
public class BriefingPrompts(IPromptRetrieverService promptRetrieverService)
{
    [McpServerPrompt(Name = "BriefingSystemInstructionPrompt", Title = "Briefing system instruction prompt"), Description("Generates a briefing system instruction prompt.")]
    public string BriefingSystemInstructionPrompt()
    {
        var prompt = promptRetrieverService.GetPrompt(PromptType.SystemInstruction);

        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("System instruction prompt is missing or empty.");

        return prompt;
    }
}
