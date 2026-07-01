using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Primitives.Prompts;
using Dfe.Mcp.Server.Application.Primitives.Resources;
using Dfe.Mcp.Server.Application.Primitives.Tools;
using Dfe.Mcp.Server.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

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
}
