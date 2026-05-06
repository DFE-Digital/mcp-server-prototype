using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Primitives.Prompts;
using Dfe.Mcp.Server.Application.Primitives.Resources;
using Dfe.Mcp.Server.Application.Primitives.Tools;

namespace Dfe.Mcp.Server.Web.Extensions;

public static class McpServerExtensions
{
    /// <summary>
    /// Add MCP server configurations
    /// </summary>
    /// <param name="services"></param>
    /// <returns>An instance of <see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddMcpServerConfigured(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();

        var mcpServerOptions = provider.GetRequiredService<McpServerConfiguration>(); 
        services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new()
                {
                    Name = mcpServerOptions.Name,
                    Version = mcpServerOptions.Version,
                    Description = mcpServerOptions.Description,
                    Title = mcpServerOptions.Title
                };
            })
            .WithHttpTransport(options =>
            {
                options.Stateless = mcpServerOptions.IsStateless;
            })
            .WithToolsFromAssembly(typeof(AzureAISearchTools).Assembly)
            .WithResourcesFromAssembly(typeof(OfstedRatingResource).Assembly)
            .WithPromptsFromAssembly(typeof(Prompts).Assembly)
            .AddAuthorizationFilters();

        return services;
    }
}
