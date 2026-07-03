using Dfe.Mcp.Server.Application.Helpers;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Dfe.Mcp.Server.Data;
using Dfe.Mcp.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dfe.Mcp.Server.Application.Services;

public class AcademiesQueryService(ILogger<AcademiesQueryService> logger, IDbContextFactory<AcademiesDbContext> dbContextFactory) : IAcademiesQueryService
{
    public async Task<ResponseModel> RunQueryAsync(EstablishmentQueryModel parameters)
    {
        using var context = dbContextFactory.CreateDbContext();
        try
        {
            var query = context.Establishments.AsNoTracking().AsQueryable();

            if (parameters.Urn.HasValue)
                query = query.Where(e => e.Urn == parameters.Urn);

            if (!string.IsNullOrWhiteSpace(parameters.Name))
                query = query.Where(e => e.SchoolName == parameters.Name);

            if (!string.IsNullOrWhiteSpace(parameters.LocalAuthority))
                query = query.Where(e => e.LocalAuthority == parameters.LocalAuthority);

            if (!string.IsNullOrWhiteSpace(parameters.OfstedRegion))
                query = query.Where(e => e.OfstedRegion == parameters.OfstedRegion);

            if (!string.IsNullOrWhiteSpace(parameters.OfstedPhase))
                query = query.Where(e => e.OfstedPhase == parameters.OfstedPhase);

            if (!string.IsNullOrWhiteSpace(parameters.OverallEffectiveness))
                query = query.Where(e => e.OverallEffectiveness == parameters.OverallEffectiveness);

            if (!string.IsNullOrWhiteSpace(parameters.Postcode))
                query = query.Where(e => e.Postcode!.StartsWith(parameters.Postcode));

            if (parameters.SafeguardingIsEffective.HasValue)
            {
                var value = parameters.SafeguardingIsEffective.Value ? "Yes" : "No";
                query = query.Where(e => e.SafeguardingIsEffective == value);
            }

            if (parameters.MinPupils.HasValue)
                query = query.Where(e => e.TotalNumberOfPupils >= parameters.MinPupils);

            if (parameters.MaxPupils.HasValue)
                query = query.Where(e => e.TotalNumberOfPupils <= parameters.MaxPupils);

            var establishments = await query
                .OrderBy(e => e.SchoolName)
                .Take(parameters.Limit)
                .ToListAsync();

            return new ResponseModel(JsonHelper.Serialize(establishments), establishments.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while running query with parameters: {Parameters}", JsonHelper.Serialize(parameters));
            return new ResponseModel(Error: ex.Message);
        }
    }
}
