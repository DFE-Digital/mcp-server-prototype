using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Domain;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using static ModelContextProtocol.Protocol.ElicitRequestParams;

namespace Dfe.Mcp.Server.Application.Primitives.Tools;

[McpServerToolType]
public sealed class AzureAISearchElicitationTools(AzureAISearchTools searchTools, McpServer server)
{ 

    [McpServerTool(Name = "elicit_search_ofsted", Title = "Search Ofsted (guided)", ReadOnly = true)]
    [Description("Interactively collects search parameters from the user, then searches Ofsted information.")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<ResponseModel> ElicitSearchOfsted(CancellationToken cancellationToken = default)
    {
        if (server.ClientCapabilities?.Elicitation is null)
            return new ResponseModel(Error: ErrorMessages.OfstedInteractiveInputNotSupported);

        var schema = new RequestSchema
        {
            Properties = new Dictionary<string, PrimitiveSchemaDefinition>
            {
                ["query"] = new StringSchema { Description = "Full-text search query. Use '*' to return all records." },
                ["top"] = new NumberSchema { Description = "Maximum results to return (1–50). Default: 10." },
                ["filter"] = new StringSchema { Description = "OData filter, e.g. \"OverallEffectiveness eq 1\". Leave empty for no filter." },
                ["select"] = new StringSchema { Description = "Comma-separated field names to return. Leave empty for all fields." },
            }
        };

        var result = await server.ElicitAsync(
            new ElicitRequestParams { Message = ErrorMessages.InvalidSearchParameters, RequestedSchema = schema },
            cancellationToken);

        if (result.Action != "accept")
            return new ResponseModel(Error: ErrorMessages.CancelledSearch);

        return await searchTools.SearchOfsted(
            query: GetString(result.Content, "query", "*"),
            top: GetInt(result.Content, "top", 10),
            filter: GetStringOrNull(result.Content, "filter"),
            select: GetStringOrNull(result.Content, "select"),
            cancellationToken: cancellationToken);
    }
     
    [McpServerTool(Name = "elicit_search_establishment", Title = "Search Establishment or school or academy (guided)", ReadOnly = true)]
    [Description("Interactively collects search parameters from the user, then searches establishment, school, academy, or trust information.")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<ResponseModel> ElicitSearchEstablishment(CancellationToken cancellationToken = default)
    {
        if (server.ClientCapabilities?.Elicitation is null)
            return new ResponseModel(Error: ErrorMessages.EstablishmentInteractiveInputNotSupported);

        var schema = new RequestSchema
        {
            Properties = new Dictionary<string, PrimitiveSchemaDefinition>
            {
                ["query"] = new StringSchema { Description = "Full-text search query. Use '*' to return all records." },
                ["top"] = new NumberSchema { Description = "Maximum results to return (1–50). Default: 10." },
                ["filter"] = new StringSchema { Description = "OData filter, e.g. \"TypeOfEstablishment eq 'Academy'\". Leave empty for no filter." },
                ["select"] = new StringSchema { Description = "Comma-separated field names, e.g. \"URN,EstablishmentName,Postcode\". Leave empty for all fields." },
            }
        };

        var result = await server.ElicitAsync(
            new ElicitRequestParams { Message = ErrorMessages.InvalidSearchParameters, RequestedSchema = schema },
            cancellationToken);

        if (result.Action != "accept")
            return new ResponseModel(Error: ErrorMessages.CancelledSearch);

        return await searchTools.SearchEstablishment(
            query: GetString(result.Content, "query", "*"),
            top: GetInt(result.Content, "top", 10),
            filter: GetStringOrNull(result.Content, "filter"),
            select: GetStringOrNull(result.Content, "select"),
            cancellationToken: cancellationToken);
    }
     
    private static string GetString(IDictionary<string, JsonElement>? content, string key, string fallback) =>
        content is not null && content.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? fallback
            : fallback;

    private static string? GetStringOrNull(IDictionary<string, JsonElement>? content, string key) =>
        content is not null && content.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static int GetInt(IDictionary<string, JsonElement>? content, string key, int fallback) =>
        content is not null && content.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetInt32()
            : fallback;
}