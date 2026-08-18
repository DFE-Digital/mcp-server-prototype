using Dfe.Mcp.Server.Application.Enums;

namespace Dfe.Mcp.Server.Application.Configurations;

public class PromptConfiguration
{
    public Dictionary<SystemPromptType, string> SystemPrompts { get; set; } = []; 
}
