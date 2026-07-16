using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Services;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Dfe.Mcp.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Dfe.Mcp.Server.Web.Extensions;

public static class DatabaseConfigurationExtensions
{
    /// <summary>
    /// Add database configurations
    /// </summary>
    /// <param name="services"></param>
    /// <returns>An instance of <see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddDatabaseConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = services.BuildServiceProvider();
        
        services.AddDbContextFactory<AcademiesDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("AcademiesConnection")));

        return services;
    }

    /// <summary>
    /// Add Databricks configurations
    /// </summary>
    /// <param name="services"></param>
    /// <returns>An instance of <see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddDatabricksConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        var databricksOptions = configuration.GetSection("Databricks").Get<DatabricksConfiguration>()
       ?? throw new InvalidOperationException("Databricks section is missing!");
        
        services.AddSingleton(databricksOptions);

        services.AddHttpClient<IDatabricksSqlService, DatabricksSqlService>((sp, client) =>
        {
            client.BaseAddress = new Uri(databricksOptions.WorkspaceUrl);
        });

        return services;
    }
}
