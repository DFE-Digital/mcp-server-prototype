namespace Dfe.Mcp.Server.Domain;

public class OfstedRatingModel
{
    public int Id { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string OverallEffectiveness { get; set; } = string.Empty; // e.g. Outstanding, Good
    public string QualityOfEducation { get; set; } = string.Empty;
    public string BehaviourAndAttitudes { get; set; } = string.Empty;
    public string LeadershipAndManagement { get; set; } = string.Empty;
    public DateTime InspectionDate { get; set; }
}
