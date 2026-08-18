using System.Net;
using System.Text;
using System.Text.Json;

namespace Dfe.Mcp.Server.Web.Tests.Integration;

/// <summary>
/// Covers the plain HTTP surface the MCP server exposes alongside the protocol endpoint: the
/// landing page, the health check, and the status codes returned for unauthenticated or malformed
/// requests to <c>/mcp</c>.
/// </summary>
public class McpServerConfigurationTests : IAsyncLifetime
{
    private McpServerWebApplicationFactory _factory = null!;
    private HttpClient _httpClient = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        _factory = new McpServerWebApplicationFactory();
        _httpClient = _factory.CreateClient();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task HealthCheck_ReturnsOkAsPlainTextOrJson()
    {
        // Action
        var response = await _httpClient.GetAsync("/health", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        Assert.True(
            mediaType is "text/plain" or "application/json",
            $"Expected text/plain or application/json, got {mediaType}");
    }

    [Fact]
    public async Task RootEndpoint_ReturnsTheServerInfoDocument()
    {
        // Action
        var response = await _httpClient.GetAsync("/", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        var root = document.RootElement;

        Assert.Equal("RSD_MCP_Server", root.GetProperty("name").GetString());
        Assert.Equal("RSD MCP Server", root.GetProperty("title").GetString());
        Assert.Equal("/mcp", root.GetProperty("mcp").GetString());
        Assert.Equal("/health", root.GetProperty("health").GetString());

        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("description").GetString()));

        var supported = root.GetProperty("supported");
        Assert.Equal(JsonValueKind.Array, supported.ValueKind);
        Assert.True(supported.GetArrayLength() > 0, "Supported capabilities should not be empty.");
    }

    [Fact]
    public async Task McpEndpoint_WithoutAuthorization_IsRejected()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","method":"tools/list","id":1}""",
                Encoding.UTF8,
                "application/json")
        };

        // Action
        var response = await _httpClient.SendAsync(request, CancellationToken);

        // Assert
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected Unauthorized or Forbidden, got {response.StatusCode}");
    }

    [Fact]
    public async Task InvalidHttpMethod_OnMcpEndpoint_IsRejected()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");

        // Action
        var response = await _httpClient.SendAsync(request, CancellationToken);

        // Assert
        Assert.True(
            response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.BadRequest
                or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected an error response, got {response.StatusCode}");
    }

    [Fact]
    public async Task NonExistentEndpoint_Returns404()
    {
        // Action
        var response = await _httpClient.GetAsync("/nonexistent", CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
