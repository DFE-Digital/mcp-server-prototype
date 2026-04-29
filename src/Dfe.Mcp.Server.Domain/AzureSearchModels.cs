namespace Dfe.Mcp.Server.Domain;

/// <summary>
/// Represents a single document returned from an Azure AI Search index.
/// Fields are stored in a dynamic dictionary so the model works across
/// any index schema without code changes.
/// </summary>
public sealed class AzureSearchResultDocument
{
    /// <summary>Azure AI Search score (relevance).</summary>
    public double? Score { get; init; }

    /// <summary>All document fields returned by the search query.</summary>
    public Dictionary<string, object?> Fields { get; init; } = [];
}

/// <summary>
/// Wrapper returned by <see cref="Services.AzureSearchService"/>.
/// </summary>
public sealed class AzureSearchResponse
{
    /// <summary>The query that was executed.</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>Total approximate match count (if available).</summary>
    public long? TotalCount { get; init; }

    /// <summary>Documents returned.</summary>
    public List<AzureSearchResultDocument> Results { get; init; } = [];

    /// <summary>Any error message — null on success.</summary>
    public string? Error { get; init; }
}
