using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Dfe.Mcp.Server.Domain;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using System.ComponentModel;

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
    public async Task<ResponseModel> SearchOfsted(
        [Description("Full-text search query. Use '*' to return all records. Supports Lucene syntax e.g. 'outstanding AND primarySchool'.")] string query,
        [Description("Maximum number of results to return (1–50). Default: 10.")] int top = 10, 
        [Description("OData filter expression, e.g. \"OverallEffectiveness eq 1\". Leave empty for no filter.")] string? filter = null,
        [Description("Comma-separated list of field names to return. Leave empty for all fields.")] string? select = null,
        CancellationToken cancellationToken = default)
    {
        top = Math.Clamp(top, 1, 50);
        return await searchService.SearchOfstedAsync(query, top, filter, ExtractSelectFields(select), cancellationToken);
    }

    [McpServerTool(Name = "search_establishment", UseStructuredContent = true, Title = "Search Establishment or school or academy", ReadOnly = true),
        Description("Search the establishment or school or academy or trust information.")]
    [Authorize(Policy = Policy.ToolsAccess)] 
    public async Task<ResponseModel> SearchEstablishment(
        [Description("Full-text search query. Use '*' to return all records. Supports Lucene syntax e.g. 'Greenfield AND primary'.")] string query, 
        [Description("Maximum number of results to return (1–50). Default: 10.")] int top = 10, 
        [Description("OData filter expression, e.g. \"TypeOfEstablishment eq 'Academy'\". Leave empty for no filter.")] string? filter = null, 
        [Description("Comma-separated list of field names to return. Leave empty for all fields. e.g. \"URN,EstablishmentName,Postcode\"")] string? select = null, 
        CancellationToken cancellationToken = default)
    {
        top = Math.Clamp(top, 1, 50);

        return await searchService.SearchEstablishmentAsync(query, top, filter, ExtractSelectFields(select), cancellationToken);
    }

    [McpServerTool(Name = "search_recast_concerns", UseStructuredContent = true, Title = "Search Recast concerns for the establishment or trust", ReadOnly = true),
        Description("Search the recast concerns.")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<ResponseModel> SearchRecastConcerns(
        [Description("Full-text search query. Use '*' to return all concerns. Supports Lucene syntax e.g. 'Financial goverance AND project deficit'.")] string query,
        [Description("Maximum number of results to return (1–50). Default: 10.")] int top = 10,
        [Description("OData filter expression, e.g. \"TypeOfEstablishment eq 'Academy'\". Leave empty for no filter.")] string? filter = null,
        [Description("Comma-separated list of field names to return. Leave empty for all fields. e.g. \"Case_id\"")] string? select = null,
        CancellationToken cancellationToken = default)
    {
        top = Math.Clamp(top, 1, 50);

        return await searchService.SearchRecastConcernsAsync(query, top, filter, ExtractSelectFields(select), cancellationToken);
    }
    [McpServerTool(Name = "search_rise_concerns", UseStructuredContent = true, Title = "Search rise concerns for the establishment or trust", ReadOnly = true),
        Description("Search the rise concerns.")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<ResponseModel> SearchRiseConcerns(
        [Description("Full-text search query. Use '*' to return all concerns. Supports Lucene syntax e.g. 'Greenfield AND primary'.")] string query,
        [Description("Maximum number of results to return (1–50). Default: 10.")] int top = 10,
        [Description("OData filter expression, e.g. \"TypeOfEstablishment eq 'Academy'\". Leave empty for no filter.")] string? filter = null,
        [Description("Comma-separated list of field names to return. Leave empty for all fields. e.g. \"Case_id\"")] string? select = null,
        CancellationToken cancellationToken = default)
    {
        top = Math.Clamp(top, 1, 50);

        return await searchService.SearchRiseConcernsAsync(query, top, filter, ExtractSelectFields(select), cancellationToken);
    }
    private static string[]? ExtractSelectFields(string? select) => string.IsNullOrWhiteSpace(select)
            ? null
            : select.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
