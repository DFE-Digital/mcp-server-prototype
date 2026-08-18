using Azure.Search.Documents;
using GovUK.Dfe.CoreLibs.SharePoint;
using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.FileRetrievers;
using Dfe.Mcp.Server.Application.FileRetrievers.Interfaces;
using Dfe.Mcp.Server.Application.Services;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Server;

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
        services.AddAuthenticationAndAuthorization(config);
        services.AddDatabaseConfigurations(config);
        services.AddDatabricksConfigurations(config);

        // SharePoint
        services.AddSharePointServices(config, "SharePoint");

        // Health check 
        services.AddHealthChecks();

        // CORS 
        services.AddCorsPolicies(config);

        // MCP Server
        services.AddMcpServerConfigured();

        return services;
    }
    /// <summary>
    /// Sets Cors policies
    /// </summary>
    /// <param name="services">An instance of <see cref="IServiceCollection"/> </param>
    /// <param name="config">An instance of <see cref="IConfiguration"/> </param>
    /// <returns>An instance of <see cref="IServiceCollection"/></returns>
    private static IServiceCollection AddCorsPolicies(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins")
       .Get<string[]>() ?? [];
        services.AddCors(cors =>
        {
            cors.AddPolicy(InfrastructureConfiguration.CorsPolicyName, policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials());
        });

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

        var promptFiles = config.GetSection("PromptFiles").Get<PromptConfiguration>()
            ?? throw new InvalidOperationException("Prompt files section is missing!");

        var mcpServerOptions = config.GetSection("McpServer").Get<McpServerConfiguration>()
            ?? throw new InvalidOperationException("McpServer section is missing!");

        services.AddSingleton(azureSearchOptions);
        services.AddSingleton(promptFiles);
        services.AddSingleton(mcpServerOptions);

        return services;
    }

    /// <summary>
    /// Add MCP services dependency injection
    /// </summary>
    /// <param name="services"></param>
    /// <returns>An instance of <see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddMcpServices(this IServiceCollection services)
    {
        services.AddSingleton(new SearchClientOptions());
        services.AddScoped<IAzureSearchService, AzureSearchService>();
        services.AddScoped<IPromptFileReader, PromptFileReader>();
        services.AddScoped<IPromptRetrieverService, PromptRetrieverService>();
        services.AddScoped<IAcademiesQueryService, AcademiesQueryService>();
        services.AddScoped<IDatabricksSqlService, DatabricksSqlService>();
        services.AddScoped<IDateTimeService, DateTimeService>();
        services.AddScoped<ISharePointDocumentService, SharePointDocumentService>();
        return services;
    }

    private static IServiceCollection SetPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            var policies = new[]
            {
                (Policy.ToolsAccess,    McpScope.ReadTools,    McpRole.ReadTools),
                (Policy.ResourceAccess, McpScope.ReadResource, McpRole.ReadResource),
                (Policy.PromptAccess,   McpScope.ReadPrompts,  McpRole.ReadPrompts),
            };

            foreach (var (name, scope, role) in policies)
                options.AddPolicy(name, policy => policy.RequireAssertion(ctx => HasAccess(ctx, scope, role)));
        });

        static bool HasAccess(AuthorizationHandlerContext ctx, string scope, string role) =>
            HasScope(ctx, Claim.ScopeName, scope) ||
            HasScope(ctx, Claim.ScopeUrl, scope) ||
            ctx.User.IsInRole(role) ||
            ctx.User.HasClaim(Claim.RoleName, role);

        // Entra ID issues every consented scope in a single space delimited "scp" claim, so the
        // claim value has to be split before comparing. Matching the whole value would only ever
        // authorise a token that carries exactly one scope.
        static bool HasScope(AuthorizationHandlerContext ctx, string claimType, string scope) =>
            ctx.User.FindAll(claimType)
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Contains(scope, StringComparer.Ordinal);

        return services;
    }

    private static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration config)
    {
        var tenantId = config["AzureAd:TenantId"];
        var authorizationServer = $"{config["AzureAd:Instance"]}{tenantId}/v2.0";
        var serverBaseUrl = config["ServerBaseUrl"];

        services
       .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddMicrosoftIdentityWebApi(config.GetSection("AzureAd"))
       .EnableTokenAcquisitionToCallDownstreamApi()
       .AddInMemoryTokenCaches()
       .Services
       .AddAuthentication()
       .AddMcp(options =>
       {
           options.ResourceMetadata = new ProtectedResourceMetadata
           {
               Resource = serverBaseUrl,
               AuthorizationServers = { authorizationServer },
               ScopesSupported = [McpRole.ReadTools, McpRole.ReadResource, McpRole.ReadPrompts]
           };
       });

        SetPolicies(services);

        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnChallenge = ctx =>
                {
                    ctx.HandleResponse();
                    ctx.Response.StatusCode = 401;
                    ctx.Response.ContentType = "application/json";
                    return ctx.Response.WriteAsync(
                        """{"error":"unauthorized","detail":"Bearer token required"}""");
                },
                OnForbidden = ctx =>
                {
                    ctx.Response.StatusCode = 403;
                    ctx.Response.ContentType = "application/json";
                    return ctx.Response.WriteAsync(
                        """{"error":"forbidden","detail":"Insufficient scope"}""");
                }
            };
        });

        return services;
    }
}
