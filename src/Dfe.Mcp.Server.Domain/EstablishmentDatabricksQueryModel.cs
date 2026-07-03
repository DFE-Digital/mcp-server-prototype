namespace Dfe.Mcp.Server.Domain;

public class EstablishmentDatabricksQueryModel
{
    public string? GroupId { get; set; }
    public string? Urn { get; set; }
    public string? UKPRN { get; set; }
    public string? EstablishmentName { get; set; }
    public string? EstablishmentPhase { get; set; }
    public string? TrustName { get; set; }
    public int Limit { get; set; } = 5;
}
