using Dfe.Mcp.Server.Domain;

namespace Dfe.Mcp.Server.Application.Services.Interfaces;

public interface IAzureSearchService
{
    Task<ResponseModel> SearchAsync(string indexKey, string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches using the establishments index.
    /// </summary>
    Task<ResponseModel> SearchEstablishmentAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches using the ofsted index.
    /// </summary>
    Task<ResponseModel> SearchOfstedAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches using the recast concern index.
    /// </summary>
    Task<ResponseModel> SearchRecastConcernsAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches using the rise concern index.
    /// </summary>
    Task<ResponseModel> SearchRiseConcernsAsync(string query, int? top = null, string? filter = null, IEnumerable<string>? select = null, CancellationToken cancellationToken = default);
}
