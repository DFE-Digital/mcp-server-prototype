namespace Dfe.Mcp.Server.Domain;

public class EstablishmentQueryModel
{
    public int? Urn { get; set; }
    public string? Name { get; set; }
    public string? LocalAuthority { get; set; }
    public string? OfstedRegion { get; set; }
    public string? OfstedPhase { get; set; }
    public string? OverallEffectiveness { get; set; }
    public string? Postcode { get; set; }
    public bool? SafeguardingIsEffective { get; set; }
    public int? MinPupils { get; set; }
    public int? MaxPupils { get; set; }
    public int Limit { get; set; } = 20;
}
