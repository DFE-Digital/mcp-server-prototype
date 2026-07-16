namespace Dfe.Mcp.Server.Domain;

/// <summary>
/// Represents a standard response model for API responses, containing the result, total count, and any error message.
/// </summary>
/// <param name="Results">The result data.</param>
/// <param name="TotalCount">The total count of matching records.</param>
/// <param name="Error">Any error message — null on success.</param>
public record ResponseModel(
    string? Results = null,
    long? TotalCount = 0,
    string? Error = null);
