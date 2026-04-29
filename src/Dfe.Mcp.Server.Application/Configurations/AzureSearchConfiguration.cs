namespace Dfe.Mcp.Server.Application.Configurations;

public sealed class AzureSearchConfiguration
{
    public const string SectionName = "AzureSearch";
    public string Endpoint { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty; 
    public Dictionary<string, string> Indexes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public int DefaultTop { get; init; } = 10; 
    public List<string> DefaultSelect { get; init; } = [];
}
