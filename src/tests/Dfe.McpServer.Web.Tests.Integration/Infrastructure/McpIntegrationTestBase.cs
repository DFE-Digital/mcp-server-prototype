using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Helpers;
using Dfe.Mcp.Server.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;

/// <summary>
/// Base class for integration tests that run a real MCP server and connect to it with a real MCP client.
/// </summary>
public abstract class McpIntegrationTestBase : IAsyncLifetime
{
    /// <summary>All scopes the MCP primitives can require.</summary>
    protected static readonly string[] AllScopes =
        [McpScope.ReadTools, McpScope.ReadResource, McpScope.ReadPrompts];

    private readonly List<McpClient> _clients = [];
    private readonly List<HttpClient> _httpClients = [];

    protected McpServerWebApplicationFactory Factory { get; private set; } = null!;

    protected FakeAzureSearchApi AzureSearchApi => Factory.AzureSearchApi;

    protected FakeDatabricksApi DatabricksApi => Factory.DatabricksApi;

    /// <summary>The fake SharePoint document store the real SharePoint document service talks to.</summary>
    protected FakeSharePointApi SharePointApi => Factory.SharePointApi;

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>
    /// Whether the MCP HTTP transport runs stateless. <see langword="null"/> keeps the application's
    /// own configuration. Stateless servers cannot make server-to-client requests, so elicitation is
    /// only available when this returns <see langword="false"/>.
    /// </summary>
    protected virtual bool? Stateless => null;

    /// <summary>Whether the academies SQL database is replaced with an EF Core in-memory database.</summary>
    protected virtual bool UseInMemoryAcademiesDatabase => false;

    public virtual ValueTask InitializeAsync()
    {
        Factory = new McpServerWebApplicationFactory();

        if (Stateless.HasValue)
            Factory.WithStatelessTransport(Stateless.Value);

        if (UseInMemoryAcademiesDatabase)
            Factory.WithInMemoryAcademiesDatabase();

        return ValueTask.CompletedTask;
    }

    public virtual async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
            await client.DisposeAsync();

        foreach (var httpClient in _httpClients)
            httpClient.Dispose();

        Factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Connects a real MCP client to the running server, performing the full initialize handshake.
    /// </summary>
    /// <param name="scopes">Scope claims granted to the caller. Defaults to all MCP scopes.</param>
    /// <param name="clientOptions">Optional client options, e.g. to supply an elicitation handler.</param>
    /// <param name="roles">Role claims granted to the caller.</param>
    protected async Task<McpClient> CreateMcpClientAsync(
        string[]? scopes = null,
        McpClientOptions? clientOptions = null,
        string[]? roles = null)
    {
        var httpClient = Factory.CreateClientWithClaims(scopes ?? AllScopes, roles ?? []);
        _httpClients.Add(httpClient);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            },
            httpClient,
            NullLoggerFactory.Instance,
            ownsHttpClient: false);

        var client = await McpClient.CreateAsync(
            transport,
            clientOptions,
            NullLoggerFactory.Instance,
            CancellationToken);

        _clients.Add(client);
        return client;
    }

    /// <summary>Concatenates the text content blocks of a tool result.</summary>
    protected static string TextOf(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    /// <summary>Asserts the tool call completed without an error result. <c>IsError</c> is null on success.</summary>
    protected static void AssertToolSucceeded(CallToolResult result) =>
        Assert.True(result.IsError is not true, $"Tool call reported an error: {TextOf(result)}");

    /// <summary>
    /// Reads the <see cref="ResponseModel"/> a tool returned, from structured content when the tool
    /// opts into it and from the serialised text block otherwise.
    /// </summary>
    protected static ResponseModel ResponseModelOf(CallToolResult result)
    {
        var json = result.StructuredContent?.GetRawText() ?? TextOf(result);

        var responseModel = JsonHelper.Deserialize<ResponseModel>(json);

        Assert.NotNull(responseModel);
        return responseModel;
    }

    /// <summary>Parses the <c>Results</c> payload of a tool response as a JSON array.</summary>
    protected static JsonElement[] ResultsArrayOf(ResponseModel response)
    {
        Assert.NotNull(response.Results);
        return JsonSerializer.Deserialize<JsonElement[]>(response.Results)!;
    }
}
