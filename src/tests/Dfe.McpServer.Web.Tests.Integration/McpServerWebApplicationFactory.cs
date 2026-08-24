using Azure.Core.Pipeline;
using Azure.Search.Documents;
using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Services;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Dfe.Mcp.Server.Data;
using Dfe.Mcp.Server.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.AspNetCore;
using Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;
using GovUK.Dfe.CoreLibs.SharePoint.Interfaces;
using GovUK.Dfe.CoreLibs.SharePoint.Settings;

namespace Dfe.Mcp.Server.Web.Tests.Integration;

/// <summary>
/// WebApplicationFactory for integration testing the MCP server. Runs the application for real and
/// swaps only the outbound third-party APIs and the academies database.
/// </summary>
public class McpServerWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string OfstedIndexName = "ofsted-index";
    public const string EstablishmentIndexName = "establishment-index";
    public const string RecastConcernsIndexName = "recast-concerns-index";
    public const string RiseConcernsIndexName = "rise-concerns-index";
    public const string FakeAzureSearchApiKey = "fake-search-api-key";
    public const string FakeDatabricksWorkspaceUrl = "https://fake.databricks.net";
    public const string FakeDatabricksWarehouseId = "fake-warehouse-id";
    public const string FakeDatabricksAccessToken = "fake-databricks-token";

    /// <summary>Site path the mocked SharePoint app configuration uses when building folder paths.</summary>
    public const string FakeSharePointSitePath = "/sites/fake-trusts/";

    /// <summary>Host name in the mocked SharePoint app configuration.</summary>
    public const string FakeSharePointSiteHostname = "fake.sharepoint.com";

    private readonly string _academiesDatabaseName = $"academies-{Guid.NewGuid():N}";
    private bool? _stateless;
    private bool _useInMemoryAcademiesDatabase;

    public FakeAzureSearchApi AzureSearchApi { get; } = new();

    public FakeDatabricksApi DatabricksApi { get; } = new();

    /// <summary>The fake SharePoint document store the real SharePoint document service talks to.</summary>
    public FakeSharePointApi SharePointApi { get; } = new();

    /// <summary>
    /// Overrides the streamable HTTP transport mode. The server defaults to stateless, which disables
    /// server-to-client requests; a stateful server is required to exercise elicitation.
    /// </summary>
    /// <param name="stateless">Whether the MCP HTTP transport should run stateless.</param>
    /// <returns>This factory for fluent configuration</returns>
    public McpServerWebApplicationFactory WithStatelessTransport(bool stateless)
    {
        _stateless = stateless;
        return this;
    }

    /// <summary>
    /// Swaps the SQL Server backed academies database for an EF Core in-memory database so the real
    /// <see cref="AcademiesQueryService"/> can run against seeded data.
    /// </summary>
    /// <returns>This factory for fluent configuration</returns>
    public McpServerWebApplicationFactory WithInMemoryAcademiesDatabase()
    {
        _useInMemoryAcademiesDatabase = true;
        return this;
    }

    /// <summary>
    /// Seeds the in-memory academies database. Requires <see cref="WithInMemoryAcademiesDatabase"/>.
    /// </summary>
    /// <param name="establishments">The establishments to insert.</param>
    public async Task SeedEstablishmentsAsync(params MisEstablishment[] establishments)
    {
        var contextFactory = Services.GetRequiredService<IDbContextFactory<AcademiesDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync();

        context.Establishments.AddRange(establishments);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> whose requests are authenticated by
    /// <see cref="TestAuthenticationHandler"/> with the supplied scopes.
    /// </summary>
    /// <param name="scopes">The scope claim values to grant, e.g. <c>Tools.Read</c>.</param>
    public HttpClient CreateClientWithScopes(params string[] scopes) =>
        CreateClientWithClaims(scopes, roles: []);

    /// <summary>
    /// Creates an <see cref="HttpClient"/> whose requests are authenticated by
    /// <see cref="TestAuthenticationHandler"/> with the supplied scopes and roles.
    /// </summary>
    /// <param name="scopes">The scope claim values to grant.</param>
    /// <param name="roles">The role claim values to grant.</param>
    /// <param name="userName">The name of the authenticated principal.</param>
    public HttpClient CreateClientWithClaims(string[] scopes, string[] roles, string userName = "integration-test-user")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHeaders.Auth, userName);

        if (scopes.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHeaders.Scopes, string.Join(',', scopes));

        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHeaders.Roles, string.Join(',', roles));

        return client;
    }

    private static Dictionary<string, string?> TestConfiguration() => new()
    {
        // Azure Search
        ["AzureSearch:Endpoint"] = "https://test.search.windows.net",
        ["AzureSearch:ApiKey"] = "test-api-key",
        ["AzureSearch:Indexes:Ofsted"] = "ofstedindex",
        ["AzureSearch:Indexes:Establishment"] = "establishmentindex",
        ["AzureSearch:Indexes:RecastConcerns"] = "recastconcernsindex",
        ["AzureSearch:Indexes:RiseConcerns"] = "riseconcernsindex",

        // Prompt Files
        ["PromptFiles:SystemPrompts:McpGovernance"] = "Prompts/MCP_Governance_System_Prompt_Addendum.md",

        // Azure AD
        ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
        ["AzureAd:ClientId"] = "test-client-id",
        ["AzureAd:TenantId"] = "test-tenant-id",
        ["AzureAd:Audience"] = "test-audience",

        // Server
        ["ServerBaseUrl"] = "https://localhost:7146",

        // CORS
        ["Cors:AllowedOrigins:0"] = "http://localhost:6274",
        ["Cors:AllowedOrigins:1"] = "https://localhost:7146",

        // Connection Strings
        ["ConnectionStrings:AcademiesConnection"] = "test-connection-string",

        // Databricks
        ["Databricks:WorkspaceUrl"] = "https://test.databricks.com",
        ["Databricks:WarehouseId"] = "test-warehouse-id",
        ["Databricks:AccessToken"] = "test-access-token",
        ["Databricks:WaitTimeOut"] = "0s",

        // SharePoint
        ["SharePoint:TenantId"] = "test-tenant-id",
        ["SharePoint:ClientId"] = "test-client-id",
        ["SharePoint:ClientSecret"] = "test-client-secret",
        ["SharePoint:SiteHostname"] = "test.sharepoint.com",
        ["SharePoint:SitePath"] = "/sites/test"
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear(); // Clear existing configuration sources
            config.AddInMemoryCollection(TestConfiguration());
        });

        // ConfigureTestServices runs last, so these overrides win over the application's own registrations.
        builder.ConfigureTestServices(services =>
        {
            // Swap the JWT bearer handler for a header driven one; the real authorization policies still apply.
            services
                .AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName, _ => { });

            if (_stateless.HasValue)
                services.Configure<HttpServerTransportOptions>(options => options.Stateless = _stateless.Value);

            if (_useInMemoryAcademiesDatabase)
                ReplaceAcademiesDbContextFactory(services, _academiesDatabaseName);

            PointAzureSearchAtFakeApi(services);
            PointDatabricksAtFakeApi(services);
            PointSharePointAtFakeApi(services);
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Replaces the Azure Search configuration and transport so the real
    /// <see cref="AzureSearchService"/> and the real Azure SDK
    /// pipeline run against <see cref="AzureSearchApi"/> instead of a live search service.
    /// </summary>
    private void PointAzureSearchAtFakeApi(IServiceCollection services)
    {
        services.RemoveAll<AzureSearchConfiguration>();
        services.AddSingleton(new AzureSearchConfiguration
        {
            Endpoint = "https://fake.search.windows.net",
            ApiKey = FakeAzureSearchApiKey,
            Indexes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ofsted"] = OfstedIndexName,
                ["Establishments"] = EstablishmentIndexName,
                ["RecastConcerns"] = RecastConcernsIndexName,
                ["RiseConcerns"] = RiseConcernsIndexName
            }
        });

        var searchClientOptions = new SearchClientOptions
        {
            Transport = new HttpClientTransport(new HttpClient(AzureSearchApi, disposeHandler: false))
        };
        searchClientOptions.Retry.MaxRetries = 0;

        services.RemoveAll<SearchClientOptions>();
        services.AddSingleton(searchClientOptions);
    }

    /// <summary>
    /// Replaces the Databricks configuration and the typed client's base address and primary handler
    /// so the real <see cref="Dfe.Mcp.Server.Application.Services.DatabricksSqlService"/> runs against
    /// <see cref="DatabricksApi"/> instead of a live workspace.
    /// </summary>
    private void PointDatabricksAtFakeApi(IServiceCollection services)
    {
        services.RemoveAll<DatabricksConfiguration>();
        services.AddSingleton(new DatabricksConfiguration
        {
            WorkspaceUrl = FakeDatabricksWorkspaceUrl,
            WarehouseId = FakeDatabricksWarehouseId,
            AccessToken = FakeDatabricksAccessToken,
            WaitTimeOut = "0s",
            PollIntervalMs = 1,
            QueryTimeoutSeconds = 10
        });

        // Registered last, so this typed client wins over the application's registration.
        services
            .AddHttpClient<IDatabricksSqlService, DatabricksSqlService>(client =>
                client.BaseAddress = new Uri(FakeDatabricksWorkspaceUrl))
            .ConfigurePrimaryHttpMessageHandler(() => DatabricksApi);
    }

    /// <summary>
    /// Replaces the SharePoint app configuration and client so the real
    /// <see cref="SharePointDocumentService"/> runs against <see cref="SharePointApi"/> instead of a
    /// live SharePoint site.
    /// </summary>
    private void PointSharePointAtFakeApi(IServiceCollection services)
    {
        services.RemoveAll<SharePointOptions>();
        services.AddSingleton(new SharePointOptions
        {
            TenantId = "fake-tenant-id",
            ClientId = "fake-client-id",
            ClientSecret = "fake-client-secret",
            SiteHostname = FakeSharePointSiteHostname,
            SitePath = FakeSharePointSitePath
        });

        services.RemoveAll<ISharePointService>();
        services.AddSingleton<ISharePointService>(SharePointApi);
    }

    private static void ReplaceAcademiesDbContextFactory(IServiceCollection services, string databaseName)
    {
        services.RemoveAll<IDbContextFactory<AcademiesDbContext>>();
        services.RemoveAll<DbContextOptions<AcademiesDbContext>>();
        services.RemoveAll<DbContextOptions>();

        // Option configurations are additive, so the SQL Server one must go or EF sees two providers.
        services.RemoveAll<IDbContextOptionsConfiguration<AcademiesDbContext>>();

        services.AddDbContextFactory<AcademiesDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
    }
}
