namespace Dfe.Mcp.Server.Application.Configurations;

public class McpServerConfiguration
{
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string Description { get; init; } = string.Empty;
    public string Endpoint { get; init; } = "/mcp";
    public string HealthCheckEndpoint { get; init; } = "/health";
    public bool IsStateless { get; init; } = true; 
}
