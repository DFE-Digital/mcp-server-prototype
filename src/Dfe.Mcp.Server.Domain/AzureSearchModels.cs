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