using Dfe.Mcp.Server.Domain;

namespace Dfe.Mcp.Server.Application.Services.Interfaces;

public interface IAcademiesQueryService
{
    Task<ResponseModel> RunQueryAsync(EstablishmentQueryModel parameters);
}
