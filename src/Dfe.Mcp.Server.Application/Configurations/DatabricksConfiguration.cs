namespace Dfe.Mcp.Server.Application.Configurations;

public class DatabricksConfiguration
{
    public required string WorkspaceUrl { get; init; }
    public required string AccessToken { get; init; }
    public required string WarehouseId { get; init; }
    public string WaitTimeOut { get; init; } = "0s";
    public int PollIntervalMs { get; init; } = 500;
    public int QueryTimeoutSeconds { get; init; } = 120;
}
