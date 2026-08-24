using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Helpers;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Dfe.Mcp.Server.Domain;
using Microsoft.Extensions.Logging;

namespace Dfe.Mcp.Server.Application.Services;

/// <summary>
/// Manages communication with Azure AI search.
/// </summary>
public sealed class AzureSearchService: IAzureSearchService
{
    private readonly AzureSearchConfiguration _azureSearchConfiguration;
    private readonly Dictionary<string, SearchClient> _clients;
    private readonly ILogger<AzureSearchService> _logger;

    public AzureSearchService(
        AzureSearchConfiguration azureSearchConfiguration,
        ILogger<AzureSearchService> logger,
        SearchClientOptions searchClientOptions)
    {
        _azureSearchConfiguration = azureSearchConfiguration;
        _logger = logger;
         
        var endpoint = new Uri(_azureSearchConfiguration.Endpoint);
        var credential = new AzureKeyCredential(_azureSearchConfiguration.ApiKey);

        _clients = _azureSearchConfiguration.Indexes.ToDictionary(kvp => kvp.Key,
            kvp => new SearchClient(endpoint, kvp.Value, credential, searchClientOptions));
    }

    /// <summary>Returns all configured logical index keys.</summary>
    private IReadOnlyCollection<string> IndexKeys => [.. _azureSearchConfiguration.Indexes.Keys];

    /// <summary>
    /// Executes a full-text search against the specified index.
    /// </summary>
    /// <param name="indexKey">Logical key from configuration (e.g. "Ofsted").</param>
    /// <param name="query">Search query string. Use "*" to match all documents.</param>
    /// <param name="top">Max results to return (default from config).</param>
    /// <param name="filter">OData filter expression, or null for no filter.</param>
    /// <param name="select">Fields to return, or null/empty for all fields.</param>f
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <summary>  
    public async Task<ResponseModel> SearchAsync(string indexKey, string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
    {
        if (!_clients.TryGetValue(indexKey, out var client))
        {
            return new ResponseModel
            {
                Error = $"Index key '{indexKey}' is not configured. " +
                        $"Valid keys: {string.Join(", ", IndexKeys)}"
            };
        }

        var indexName = _azureSearchConfiguration.Indexes[indexKey];
        var effectiveTop = top ?? _azureSearchConfiguration.DefaultTop;
        var effectiveSelect = (select?.ToList() is { Count: > 0 } s ? s : _azureSearchConfiguration.DefaultSelect)
                              .ToList();

        try
        {
            var searchOptions = new SearchOptions
            {
                Size = effectiveTop, 
                Filter = filter,
                IncludeTotalCount = true,
                QueryType = SearchQueryType.Full
            };

            if (effectiveSelect.Count > 0)
                foreach (var field in effectiveSelect)
                    searchOptions.Select.Add(field);

            var response = await client.SearchAsync<SearchDocument>(
                query, searchOptions, cancellationToken);

            var documents = new List<AzureSearchResultDocument>();

            await foreach (var result in response.Value.GetResultsAsync()
                               .WithCancellation(cancellationToken))
            {
                documents.Add(new AzureSearchResultDocument
                {
                    Score = result.Score,
                    Fields = result.Document.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)!
                });
            }

            return new ResponseModel
            {
                TotalCount = response.Value.TotalCount,
                Results = JsonHelper.Serialize(documents)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for index '{Index}'", indexName);
            return new ResponseModel(Error: ex.Message);
        }
    }

    public Task<ResponseModel> SearchEstablishmentAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        => SearchAsync("Establishments", query, top, filter, select,  cancellationToken: cancellationToken);

    public Task<ResponseModel> SearchOfstedAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        => SearchAsync("Ofsted", query, top, filter, select, cancellationToken: cancellationToken);

    public Task<ResponseModel> SearchRecastConcernsAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        => SearchAsync("RecastConcerns", query, top, filter, select, cancellationToken: cancellationToken);
    public Task<ResponseModel> SearchRiseConcernsAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        => SearchAsync("RiseConcerns", query, top, filter, select, cancellationToken: cancellationToken);
}

