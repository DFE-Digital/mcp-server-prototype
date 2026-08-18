using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Primitives;

public class McpPrimitiveDiscoveryTests : McpIntegrationTestBase
{
    private static readonly string[] ExpectedTools =
    [
        "search_ofsted",
        "search_establishment",
        "elicit_search_ofsted",
        "elicit_search_establishment",
        "query_establishments_with_ofsted_data",
        "query_establishments_data"
    ];

    private static readonly string[] ExpectedResourceTemplates = ["get_latest_financial_health_assessment"];

    private static readonly string[] ExpectedPrompts = ["get_system_prompt"];

    [Fact]
    public async Task Initialize_ReturnsTheConfiguredServerInfo()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync();

        // Assert
        Assert.Equal("RSD_MCP_Server", client.ServerInfo.Name);
        Assert.Equal("RSD MCP Server", client.ServerInfo.Title);
        Assert.Equal("1.0.0", client.ServerInfo.Version);
    }

    [Fact]
    public async Task Initialize_AdvertisesTools_Resources_AndPrompts()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync();

        // Assert
        Assert.NotNull(client.ServerCapabilities.Tools);
        Assert.NotNull(client.ServerCapabilities.Prompts); 
        Assert.NotNull(client.ServerCapabilities.Resources);
    }


    /// <summary>
    /// Also guards scope splitting: the test authentication handler issues the scopes the way
    /// Entra ID does, as one space delimited claim, so this fails if the policies stop splitting them.
    /// </summary>
    [Fact]
    public async Task AllPrimitives_AreDiscoverable_WithEveryScope()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken);
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: CancellationToken);
        var prompts = await client.ListPromptsAsync(cancellationToken: CancellationToken);

        // Assert
        Assert.Equal(ExpectedTools.Order(), tools.Select(tool => tool.Name).Order());
        Assert.Equal(ExpectedResourceTemplates.Order(), templates.Select(template => template.Name).Order());
        Assert.Equal(ExpectedPrompts.Order(), prompts.Select(prompt => prompt.Name).Order());
    }

    [Fact]
    public async Task ToolsReadScope_RevealsToolsOnly()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync(scopes: [McpScope.ReadTools]);

        // Assert
        Assert.Equal(ExpectedTools.Order(), (await client.ListToolsAsync(cancellationToken: CancellationToken)).Select(tool => tool.Name).Order());
        Assert.Empty(await client.ListResourceTemplatesAsync(cancellationToken: CancellationToken));
        Assert.Empty(await client.ListPromptsAsync(cancellationToken: CancellationToken));
    }

    [Fact]
    public async Task PromptsReadScope_RevealsPromptsOnly()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync(scopes: [McpScope.ReadPrompts]);

        // Assert
        Assert.Equal(ExpectedPrompts.Order(), (await client.ListPromptsAsync(cancellationToken: CancellationToken)).Select(prompt => prompt.Name).Order());
        Assert.Empty(await client.ListToolsAsync(cancellationToken: CancellationToken));
        Assert.Empty(await client.ListResourceTemplatesAsync(cancellationToken: CancellationToken));
    }

    [Fact]
    public async Task ResourcesReadScope_RevealsResourcesOnly()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync(scopes: [McpScope.ReadResource]);

        // Assert
        Assert.Equal(ExpectedResourceTemplates.Order(), (await client.ListResourceTemplatesAsync(cancellationToken: CancellationToken)).Select(template => template.Name).Order());
        Assert.Empty(await client.ListToolsAsync(cancellationToken: CancellationToken));
        Assert.Empty(await client.ListPromptsAsync(cancellationToken: CancellationToken));
    }

    [Fact]
    public async Task NoScopesOrRoles_RevealsNothing()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync(scopes: []);

        // Assert
        Assert.Empty(await client.ListToolsAsync(cancellationToken: CancellationToken));
        Assert.Empty(await client.ListResourceTemplatesAsync(cancellationToken: CancellationToken));
        Assert.Empty(await client.ListPromptsAsync(cancellationToken: CancellationToken));
    }

    [Fact]
    public async Task Roles_GrantTheSameAccessAsScopes()
    {
        // Arrange
        var client = await CreateMcpClientAsync(
            scopes: [],
            roles: [McpRole.ReadTools, McpRole.ReadResource, McpRole.ReadPrompts]);

        // Action
        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken);
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: CancellationToken);
        var prompts = await client.ListPromptsAsync(cancellationToken: CancellationToken);

        // Assert
        Assert.Equal(ExpectedTools.Order(), tools.Select(tool => tool.Name).Order());
        Assert.Equal(ExpectedResourceTemplates.Order(), templates.Select(template => template.Name).Order());
        Assert.Equal(ExpectedPrompts.Order(), prompts.Select(prompt => prompt.Name).Order());
    }
}
