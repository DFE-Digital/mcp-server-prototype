using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Dfe.Mcp.Server.Application.Primitives.Tools;

// <summary>
/// MCP tools that expose Azure AI Search indexes results to any MCP client.
/// </summary>
[McpServerToolType]
public sealed class AzureAISearchTools(IAzureSearchService searchService)
{
    [McpServerTool(Name = "search_ofsted", UseStructuredContent = true, Title = "Search Ofsted", ReadOnly = true),
        Description("Search the Ofsted information.")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<string> SearchOfsted(
        [Description("Full-text search query. Use '*' to return all records. Supports Lucene syntax e.g. 'outstanding AND primarySchool'.")] string query,
        [Description("Maximum number of results to return (1–50). Default: 10.")] int top = 10, 
        [Description("OData filter expression, e.g. \"OverallEffectiveness eq 1\". Leave empty for no filter.")] string? filter = null,
        [Description("Comma-separated list of field names to return. Leave empty for all fields.")] string? select = null,
        CancellationToken cancellationToken = default)
    {
        top = Math.Clamp(top, 1, 50);
        var response = await searchService.SearchOfstedAsync(query, top, filter, ExtractSelectFields(select), cancellationToken);
        return JsonSerializer.Serialize(response, _jsonOptions);
    }

    [McpServerTool(Name = "search_establishment", UseStructuredContent = true, Title = "Search Establishment or school or academy", ReadOnly = true),
        Description("Search the establishment or school or academy or trust information.")]
    [Authorize(Policy = Policy.ToolsAccess)] 
    public async Task<string> SearchEstablishment(
        [Description("Full-text search query. Use '*' to return all records. Supports Lucene syntax e.g. 'Greenfield AND primary'.")] string query, 
        [Description("Maximum number of results to return (1–50). Default: 10.")] int top = 10, 
        [Description("OData filter expression, e.g. \"TypeOfEstablishment eq 'Academy'\". Leave empty for no filter.")] string? filter = null, 
        [Description("Comma-separated list of field names to return. Leave empty for all fields. e.g. \"URN,EstablishmentName,Postcode\"")] string? select = null, 
        CancellationToken cancellationToken = default)
    {
        top = Math.Clamp(top, 1, 50);

        var response = await searchService.SearchEstablishmentAsync(query, top, filter, ExtractSelectFields(select), cancellationToken);

        return JsonSerializer.Serialize(response, _jsonOptions);
    }
    private static string[]? ExtractSelectFields(string? select) => string.IsNullOrWhiteSpace(select)
            ? null
            : select.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };
}
