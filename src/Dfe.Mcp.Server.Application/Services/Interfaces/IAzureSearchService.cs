using Dfe.Mcp.Server.Domain;

namespace Dfe.Mcp.Server.Application.Services.Interfaces;

public interface IAzureSearchService
{
    Task<AzureSearchResponse> SearchAsync(string indexKey, string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience overload — searches using the establishment index.
    /// </summary>
    Task<AzureSearchResponse> SearchEstablishmentAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience overload — searches using the Ofsted index.
    /// </summary>
    Task<AzureSearchResponse> SearchOfstedAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);
}
