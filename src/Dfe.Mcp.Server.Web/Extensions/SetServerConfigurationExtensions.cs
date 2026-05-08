using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Contants; 

namespace Dfe.Mcp.Server.Web.Extensions;

public static class SetServerConfigurationExtensions
{
    /// <summary>
    /// Sets OAuth protect Resources 
    /// </summary>
    /// <param name="app">An instance of <see cref="WebApplication"/></param>
    /// <param name="config">An instance of <see cref="IConfiguration"/></param>
    /// <param name="mcpServerConfiguration">An instance of <see cref="McpServerConfiguration"/></param> 
    /// <returns>An instance of <see cref="WebApplication"/></returns>
    public static WebApplication SetServerConfiguration(this WebApplication app, IConfiguration config, McpServerConfiguration mcpServerConfiguration)
    {
        var tenantId = config["AzureAd:TenantId"];
 
        app.MapMcp(mcpServerConfiguration.Endpoint)
           .RequireAuthorization();

        app.MapGet("/", () => Results.Ok(new
        {
            name = mcpServerConfiguration.Name,
            title = mcpServerConfiguration.Title,
            version = mcpServerConfiguration.Version,
            mcp = mcpServerConfiguration.Endpoint,
            description = mcpServerConfiguration.Description,
            supported = new[] { 
                McpScope.ReadTools, McpScope.ReadResource, McpScope.ReadPrompts,
                McpRole.ReadTools, McpRole.ReadResource, McpRole.ReadPrompts,
            },
            health = mcpServerConfiguration.HealthCheckEndpoint
        }));

        return app;
    }
}
