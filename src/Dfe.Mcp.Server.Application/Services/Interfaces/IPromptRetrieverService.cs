using Dfe.Mcp.Server.Application.Enums;

namespace Dfe.Mcp.Server.Application.Services.Interfaces;

public interface IPromptRetrieverService
{
    string GetPrompt(PromptType type);
}
