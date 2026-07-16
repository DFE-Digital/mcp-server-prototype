using Dfe.Mcp.Server.Domain;

namespace Dfe.Mcp.Server.Application.Services.Interfaces;

public interface IDatabricksSqlService
{
    Task<ResponseModel> RunEstablishmentQueryAsync(EstablishmentDatabricksQueryModel model, CancellationToken ct = default);
}
