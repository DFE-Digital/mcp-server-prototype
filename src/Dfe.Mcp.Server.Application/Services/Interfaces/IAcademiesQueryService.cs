using Dfe.Mcp.Server.Data.Models;
using Dfe.Mcp.Server.Domain;

namespace Dfe.Mcp.Server.Application.Services.Interfaces;

public interface IAcademiesQueryService
{
    Task<string> QueryAsync(EstablishmentQueryModel parameters);
}
