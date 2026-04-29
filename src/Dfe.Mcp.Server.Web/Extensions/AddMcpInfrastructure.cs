using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.FileRetrievers;
using Dfe.Mcp.Server.Application.FileRetrievers.Interfaces;
using Dfe.Mcp.Server.Application.Services;
using Dfe.Mcp.Server.Application.Services.Interfaces;

namespace Dfe.Mcp.Server.Web.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add all infrastructure resources required by MCP server
    /// </summary>
    /// <param name="services">An instance of <see cref="IServiceCollection"/> </param>
    /// <param name="config">An instance of <see cref="IConfiguration"/> </param>
    /// <returns>An instance of <see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddMcpInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddMcpOptions(config);
        services.AddMcpServices(); 

        // Health check 
        services.AddHealthChecks();

        // CORS 
        services.AddCors(cors =>
            cors.AddPolicy(InfrastructureConfiguration.DevelopmentCoresPolicyName, policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod())); 


        // MCP Server
        services.AddMcpServerConfigured();
        return services;
    }
    /// <summary>
    ///  Add MCP server's configurations
    /// </summary>
    /// <param name="services">An instance of <see cref="IServiceCollection"/> </param>
    /// <param name="config">An instance of <see cref="IConfiguration"/> </param>
    /// <returns>An instance of <see cref="IServiceCollection"/></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IServiceCollection AddMcpOptions(this IServiceCollection services, IConfiguration config)
    {
        var azureSearchOptions = config.GetSection("AzureSearch").Get<AzureSearchConfiguration>()
            ?? throw new InvalidOperationException("AzureSearch section is missing!");

        var promptOptions = config.GetSection("PromptOptions").Get<PromptConfiguration>()
            ?? throw new InvalidOperationException("Prompt section is missing!");

        var mcpServerOptions = config.GetSection("McpServer").Get<McpServerConfiguration>()
            ?? throw new InvalidOperationException("McpServer section is missing!");

        var restrictedPathsOptions= config.GetSection("RestrictedPaths").Get<RestrictedPathsConfiguration>()
            ?? throw new InvalidOperationException("RestrictedPaths section is missing!");

        
        services.AddSingleton(azureSearchOptions);
        services.AddSingleton(promptOptions);
        services.AddSingleton(mcpServerOptions);
        services.AddSingleton(restrictedPathsOptions);

        return services;
    }

    /// <summary>
    /// Add MCP services dependency injection
    /// </summary>
    /// <param name="services"></param>
    /// <returns>An instance of <see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddMcpServices(this IServiceCollection services)
    {
        services.AddScoped<IAzureSearchService, AzureSearchService>();

        services.AddSingleton<IPromptFileReader, PromptFileReader>();
        services.AddSingleton<IRepositoryRetrieverService, RepositoryRetrieverService>();
        services.AddSingleton<IPromptRetrieverService, PromptRetrieverService>(); 
        return services;
    } 
}
