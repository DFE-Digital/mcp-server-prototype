using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Enums; 
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.AI; 
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Primitives.Tools;
/// <summary>
/// LLM sampling tools for Ofsted and Establishment searches.
/// These tools search the index, pass the raw results to the client-side LLM
/// via MCP sampling, and return a natural-language summary.
/// </summary>
[McpServerToolType]
public sealed class AzureAISearchSamplingTools(AzureAISearchTools searchTools, IPromptRetrieverService promptRetrieverService, McpServer server)
{
    //[McpServerTool(Name = "summarise_ofsted", Title = "Summarise Ofsted results", ReadOnly = true)]
    [Description("Searches Ofsted information and uses the LLM to return a plain-English summary of the results.")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<string> SummariseOfsted(
        [Description("Full-text search query. Use '*' to return all records.")] string query,
        [Description("Maximum number of results to pass to the LLM (1–20). Default: 5.")] int top = 5,
        [Description("OData filter expression, e.g. \"OverallEffectiveness eq 1\". Leave empty for no filter.")] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (server.ClientCapabilities?.Sampling is null)
            return "Your client does not support LLM sampling. " +
                   "Please call 'search_ofsted' directly to retrieve raw results.";

        top = Math.Clamp(top, 1, 20);
         
        var rawJson = await searchTools.SearchOfsted(query, top, filter, null, cancellationToken);

        var template = promptRetrieverService.GetUserPrompt(UserPromptType.OfstedSummaryTemplate);
        var userMessage = promptRetrieverService.Render(template, new()
        {
            ["query"] = query,
            ["rawJson"] = rawJson
        });
        ChatMessage[] messages =
        [
            new(ChatRole.System, promptRetrieverService.GetSystemPrompt(SystemPromptType.BriefingTool)),
            new(ChatRole.User, userMessage),
        ];

        var options = new ChatOptions
        {
            MaxOutputTokens = 1024,
            Temperature = 0.2f, 
        };
         
        var response = await server
            .AsSamplingChatClient()
            .GetResponseAsync(messages, options, cancellationToken);

        return response.Text ?? "No summary could be generated.";
    }
}