using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Dfe.Mcp.Server.Domain;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Primitives.Tools;

[McpServerToolType]
public sealed class DataQueryTools(IAcademiesQueryService academiesQueryService, IDatabricksSqlService databricksSqlService)
{
    [McpServerTool(Name = "query_establishments_with_ofsted_data", Title = "Query establishment, school or academy with Ofsted data"), Description(
    "Query the establishment, school or academy with ofsted data. Returns top 5 results. " +
    "Available filters: " +
    "'urn' - unique reference number of the establishment; " +
    "'name' - name of the establishment; " +
    "'localAuthority' - local authority name (e.g. 'Camden', 'Islington'); " +
    "'ofstedRegion' - Ofsted region (e.g. 'London', 'North West', 'South East'); " +
    "'ofstedPhase' - phase of education (e.g. 'Primary', 'Secondary', 'All-through', 'Nursery'); " +
    "'overallEffectiveness' - latest inspection rating ('Outstanding', 'Good', 'Requires improvement', 'Inadequate'); " +
    "'postcode' - postcode prefix to filter by area (e.g. 'SW1', 'M1', 'B12'); " +
    "'safeguardingIsEffective' - whether safeguarding is effective (true = Yes, false = No); " +
    "'minPupils' - minimum total number of pupils; " +
    "'maxPupils' - maximum total number of pupils.")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<ResponseModel> QueryEstablishmentsWithOfstedData(
    [Description("URN (Unique Reference Number) of the establishment")]
    int? urn = null,
    [Description("Name of the establishment")]
    string? name = null,
    [Description("Local authority name, e.g. 'Camden', 'Islington'")]
    string? localAuthority = null,
    [Description("Ofsted region, e.g. 'London', 'North West'")]
    string? ofstedRegion = null,
    [Description("Ofsted phase, e.g. 'Primary', 'Secondary', 'All-through'")]
    string? ofstedPhase = null,
    [Description("Overall effectiveness rating, e.g. 'Outstanding', 'Good', 'Requires improvement', 'Inadequate'")]
    string? overallEffectiveness = null,
    [Description("Postcode prefix to filter by area, e.g. 'SW1', 'M1'")]
    string? postcode = null,
    [Description("Whether safeguarding is effective: true = Yes, false = No")]
    bool? safeguardingIsEffective = null,
    [Description("Minimum total number of pupils")]
    int? minPupils = null,
    [Description("Maximum total number of pupils")]
    int? maxPupils = null,
    [Description("Max rows to return, default 5")]
    int limit = 5)
    {
        var parameters = new EstablishmentQueryModel
        {
            Urn = urn,
            Name = name,
            LocalAuthority = localAuthority,
            OfstedRegion = ofstedRegion,
            OfstedPhase = ofstedPhase,
            OverallEffectiveness = overallEffectiveness,
            Postcode = postcode,
            SafeguardingIsEffective = safeguardingIsEffective,
            MinPupils = minPupils,
            MaxPupils = maxPupils,
            Limit = Math.Clamp(limit, 1, 10)
        };

        return await academiesQueryService.RunQueryAsync(parameters);
    }

    [McpServerTool(Name = "query_establishments_data", Title = "Query establishment, school or academy"), Description(
    "Query the establishment. Returns top 5 results. " +
    "Available filters: " +
    "'urn' - unique reference number of the establishment; " +
    "'ukprn' - UK Provider Reference Number of the establishment; " +
    "'name' - name of the establishment; " +
    "'establishmentPhase' - phase of the establishment (e.g. 'Primary', 'Secondary'); " +
    "'trustName' - name of the trust; " + 
    "'groupId' - ID of the establishment group; ")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<ResponseModel> QueryEstablishmentsData(
    [Description("URN (Unique Reference Number) of the establishment")]
    string? urn = null,
    [Description("UKPRN (UK Provider Reference Number) of the establishment")]
    string? ukprn = null,
    [Description("Name of the establishment")]
    string? name = null,
    [Description("Phase of the establishment e.g. 'Primary', 'Secondary'")]
    string? establishmentPhase = null,
    [Description("Name of the trust")]
    string? TrustName = null,
    [Description("Id of the establishment group")]
    string? groupId = null,
    [Description("Max rows to return, default 5")]
    int limit = 5)
    {
        var parameters = new EstablishmentDatabricksQueryModel
        {
            Urn = urn,
            GroupId = groupId,
            EstablishmentName = name,
            EstablishmentPhase = establishmentPhase,
            UKPRN = ukprn,
            TrustName = TrustName,
            Limit = Math.Clamp(limit, 1, 10) 
        };

        return await databricksSqlService.RunEstablishmentQueryAsync(parameters);
    }
}
