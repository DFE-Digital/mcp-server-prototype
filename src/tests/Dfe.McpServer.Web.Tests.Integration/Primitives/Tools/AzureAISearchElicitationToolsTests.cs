using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Primitives.Tools;

public class AzureAISearchElicitationToolsTests : McpIntegrationTestBase
{
    private const string OfstedIndex = McpServerWebApplicationFactory.OfstedIndexName;
    private const string EstablishmentIndex = McpServerWebApplicationFactory.EstablishmentIndexName;

    /// <summary>Elicitation requires server-to-client requests, which stateless transports do not support.</summary>
    protected override bool? Stateless => false;

    [Fact]
    public async Task ListTools_ExposesElicitationTools()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken);

        // Assert
        var ofsted = Assert.Single(tools, tool => tool.Name == "elicit_search_ofsted");
        Assert.Equal("Search Ofsted (guided)", ofsted.Title);

        var establishment = Assert.Single(tools, tool => tool.Name == "elicit_search_establishment");
        Assert.Equal("Search Establishment or school or academy (guided)", establishment.Title);
    }

    [Fact]
    public async Task ElicitSearchOfsted_RequestsTheDocumentedSchemaFromTheClient()
    {
        // Arrange
        AzureSearchApi.RespondWith(OfstedIndex, totalCount: 0); 
        ElicitRequestParams? capturedRequest = null; 
        var client = await CreateMcpClientAsync(clientOptions: ElicitingClient(request =>
        {
            capturedRequest = request;
            return Accept(new Dictionary<string, JsonElement>());
        }));

        // Action
        await client.CallToolAsync("elicit_search_ofsted", cancellationToken: CancellationToken);

        // Assert
        var request = capturedRequest;
        Assert.NotNull(request);
        Assert.Equal(ErrorMessages.InvalidSearchParameters, request.Message);

        var requestedProperties = request.RequestedSchema?.Properties;
        Assert.NotNull(requestedProperties);
        Assert.Equal(["filter", "query", "select", "top"], [.. requestedProperties.Keys.Order()]);
    }

    [Fact]
    public async Task ElicitSearchOfsted_SearchesTheOfstedIndexWithTheElicitedValues()
    {
        // Arrange
        AzureSearchApi.RespondWith(OfstedIndex, totalCount: 1,
            new FakeAzureSearchApi.SearchHit(1.0, new Dictionary<string, object?> { ["URN"] = "100000" }));

        var client = await CreateMcpClientAsync(clientOptions: ElicitingClient(_ => Accept(new Dictionary<string, JsonElement>
        {
            ["query"] = JsonValue("outstanding"),
            ["top"] = JsonValue(20),
            ["filter"] = JsonValue("OverallEffectiveness eq 1"),
            ["select"] = JsonValue("URN,EstablishmentName")
        })));

        // Action
        var result = await client.CallToolAsync("elicit_search_ofsted", cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);
        Assert.Equal(1, ResponseModelOf(result).TotalCount);

        var searchRequest = AzureSearchApi.SingleRequest;
        Assert.Equal(OfstedIndex, searchRequest.IndexName);
        Assert.Equal("outstanding", searchRequest.Search);
        Assert.Equal(20, searchRequest.Top);
        Assert.Equal("OverallEffectiveness eq 1", searchRequest.Filter);
        Assert.Equal("URN,EstablishmentName", searchRequest.Select);
    }

    [Fact]
    public async Task ElicitSearchOfsted_FallsBackToDefaults_WhenTheClientOmitsValues()
    {
        // Arrange
        AzureSearchApi.RespondWith(OfstedIndex, totalCount: 0); 
        var client = await CreateMcpClientAsync( clientOptions: ElicitingClient(_ => Accept(new Dictionary<string, JsonElement>())));

        // Action
        var result = await client.CallToolAsync("elicit_search_ofsted", cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var searchRequest = AzureSearchApi.SingleRequest;
        Assert.Equal("*", searchRequest.Search);
        Assert.Equal(10, searchRequest.Top);
        Assert.Null(searchRequest.Filter);
        Assert.Null(searchRequest.Select);
    }

    [Fact]
    public async Task ElicitSearchOfsted_ClampsAnOutOfRangeElicitedTop()
    {
        // Arrange
        AzureSearchApi.RespondWith(OfstedIndex, totalCount: 0); 
        var client = await CreateMcpClientAsync(clientOptions: ElicitingClient(_ => Accept(
            new Dictionary<string, JsonElement> { ["top"] = JsonValue(500) })));

        // Action
        await client.CallToolAsync("elicit_search_ofsted", cancellationToken: CancellationToken);

        // Assert
        Assert.Equal(50, AzureSearchApi.SingleRequest.Top);
    }

    [Fact]
    public async Task ElicitSearchOfsted_ReturnsCancelled_WhenTheUserDeclines()
    {
        // Arrange
        var client = await CreateMcpClientAsync(
            clientOptions: ElicitingClient(_ => new ElicitResult { Action = "decline" }));

        // Action
        var result = await client.CallToolAsync("elicit_search_ofsted", cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);
        Assert.Equal(ErrorMessages.CancelledSearch, ResponseModelOf(result).Error);
        Assert.Empty(AzureSearchApi.Requests);
    }

    [Fact]
    public async Task ElicitSearchOfsted_ReturnsCancelled_WhenTheUserCancels()
    {
        // Arrange
        var client = await CreateMcpClientAsync(clientOptions: ElicitingClient(_ => new ElicitResult { Action = "cancel" }));

        // Action
        var result = await client.CallToolAsync("elicit_search_ofsted", cancellationToken: CancellationToken);

        // Assert
        Assert.Equal(ErrorMessages.CancelledSearch, ResponseModelOf(result).Error);
        Assert.Empty(AzureSearchApi.Requests);
    }

    [Fact]
    public async Task ElicitSearchEstablishment_SearchesTheEstablishmentIndexWithTheElicitedValues()
    {
        // Arrange
        AzureSearchApi.RespondWith(EstablishmentIndex, totalCount: 1, new FakeAzureSearchApi.SearchHit(3.0, new Dictionary<string, object?> { ["URN"] = "123456" }));
        var client = await CreateMcpClientAsync(clientOptions: ElicitingClient(_ => Accept(new Dictionary<string, JsonElement>
        {
            ["query"] = JsonValue("Greenfield"),
            ["top"] = JsonValue(15),
            ["filter"] = JsonValue("TypeOfEstablishment eq 'Academy'"),
            ["select"] = JsonValue("URN,EstablishmentName,Postcode")
        })));

        // Action
        var result = await client.CallToolAsync("elicit_search_establishment", cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);
        Assert.Equal(1, ResponseModelOf(result).TotalCount);

        var searchRequest = AzureSearchApi.SingleRequest;
        Assert.Equal(EstablishmentIndex, searchRequest.IndexName);
        Assert.Equal("Greenfield", searchRequest.Search);
        Assert.Equal(15, searchRequest.Top);
        Assert.Equal("TypeOfEstablishment eq 'Academy'", searchRequest.Filter);
        Assert.Equal("URN,EstablishmentName,Postcode", searchRequest.Select);
    }

    [Fact]
    public async Task ElicitSearchEstablishment_ReturnsCancelled_WhenTheUserDeclines()
    {
        // Arrange
        var client = await CreateMcpClientAsync(clientOptions: ElicitingClient(_ => new ElicitResult { Action = "decline" }));

        // Action
        var result = await client.CallToolAsync("elicit_search_establishment", cancellationToken: CancellationToken);

        // Assert
        Assert.Equal(ErrorMessages.CancelledSearch, ResponseModelOf(result).Error);
        Assert.Empty(AzureSearchApi.Requests);
    }

    [Fact]
    public async Task ElicitSearchOfsted_SurfacesSearchApiFailuresInTheResponsePayload()
    {
        // Arrange
        AzureSearchApi.RespondWithError(OfstedIndex, System.Net.HttpStatusCode.BadRequest, "Invalid OData filter expression.");
        var client = await CreateMcpClientAsync(clientOptions: ElicitingClient(_ => Accept(new Dictionary<string, JsonElement> { ["filter"] = JsonValue("not a filter") })));

        // Action
        var result = await client.CallToolAsync("elicit_search_ofsted", cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var response = ResponseModelOf(result);
        Assert.NotNull(response.Error);
        Assert.Contains("Invalid OData filter expression.", response.Error);
    }

    [Fact]
    public async Task ElicitationTools_AreHiddenAndRejected_WithoutToolsReadScope()
    {
        // Arrange
        var client = await CreateMcpClientAsync(
            scopes: [McpScope.ReadPrompts],
            clientOptions: ElicitingClient(_ => Accept(new Dictionary<string, JsonElement>())));

        // Action
        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken);
        
        // Assert
        Assert.DoesNotContain(tools, tool => tool.Name == "elicit_search_ofsted");
        Assert.DoesNotContain(tools, tool => tool.Name == "elicit_search_establishment");

        await Assert.ThrowsAsync<McpProtocolException>(() =>
            client.CallToolAsync("elicit_search_ofsted", cancellationToken: CancellationToken).AsTask());

        Assert.Empty(AzureSearchApi.Requests);
    }

    private static McpClientOptions ElicitingClient(Func<ElicitRequestParams?, ElicitResult> handler) => new()
    {
        Capabilities = new ClientCapabilities { Elicitation = new ElicitationCapability() },
        Handlers = new McpClientHandlers
        {
            ElicitationHandler = (request, _) => ValueTask.FromResult(handler(request))
        }
    };

    private static ElicitResult Accept(IDictionary<string, JsonElement> content) =>
        new() { Action = "accept", Content = content };

    private static JsonElement JsonValue<T>(T value) => JsonSerializer.SerializeToElement(value);
}
