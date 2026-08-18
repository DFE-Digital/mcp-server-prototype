using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;
using ModelContextProtocol;
using System.Net;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Primitives.Tools;

public class AzureAISearchToolsTests : McpIntegrationTestBase
{
    private const string OfstedIndex = McpServerWebApplicationFactory.OfstedIndexName;
    private const string EstablishmentIndex = McpServerWebApplicationFactory.EstablishmentIndexName;

    private static FakeAzureSearchApi.SearchHit ApiData(double score, params (string Key, object? Value)[] fields) =>
        new(score, fields.ToDictionary(field => field.Key, field => field.Value));

    [Fact]
    public async Task ListTools_ExposesAzureSearchTools()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        //Action
        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken);

        // Assert
        var ofsted = Assert.Single(tools, tool => tool.Name == "search_ofsted");
        Assert.Equal("Search Ofsted", ofsted.Title);
        Assert.Equal("Search the Ofsted information.", ofsted.Description);

        var establishment = Assert.Single(tools, tool => tool.Name == "search_establishment");
        Assert.Equal("Search Establishment or school or academy", establishment.Title);
    }

    [Fact]
    public async Task SearchOfsted_ReturnsDocumentsParsedFromTheSearchApiResponse()
    {
        // Arrange
        AzureSearchApi.RespondWith(OfstedIndex, totalCount: 2,
            ApiData(1.5, ("URN", "100000"), ("EstablishmentName", "Alpha School"), ("OverallEffectiveness", 1)),
            ApiData(0.9, ("URN", "100001"), ("EstablishmentName", "Beta School"), ("OverallEffectiveness", 2)));
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            "search_ofsted",
            new Dictionary<string, object?> { ["query"] = "outstanding" },
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var response = ResponseModelOf(result);
        Assert.Null(response.Error);
        Assert.Equal(2, response.TotalCount);

        var documents = ResultsArrayOf(response);
        Assert.Equal(2, documents.Length);

        Assert.Equal(1.5, documents[0].GetProperty("Score").GetDouble());
        Assert.Equal("Alpha School", documents[0].GetProperty("Fields").GetProperty("EstablishmentName").GetString());
        Assert.Equal("100000", documents[0].GetProperty("Fields").GetProperty("URN").GetString());
        Assert.Equal("Beta School", documents[1].GetProperty("Fields").GetProperty("EstablishmentName").GetString());
    }

    [Fact]
    public async Task SearchOfsted_IssuesTheExpectedRequestToTheOfstedIndex()
    {
        // Arrange
        AzureSearchApi.RespondWith(OfstedIndex, totalCount: 0); 
        var client = await CreateMcpClientAsync();

        // Action
        await client.CallToolAsync(
            "search_ofsted",
            new Dictionary<string, object?>
            {
                ["query"] = "primary",
                ["top"] = 25,
                ["filter"] = "OverallEffectiveness eq 1",
                ["select"] = "URN, EstablishmentName ,Postcode"
            },
            cancellationToken: CancellationToken);

        // Assert
        var request = AzureSearchApi.SingleRequest;
        Assert.Equal(OfstedIndex, request.IndexName);
        Assert.Equal(McpServerWebApplicationFactory.FakeAzureSearchApiKey, request.ApiKey);
        Assert.Equal("primary", request.Search);
        Assert.Equal(25, request.Top);
        Assert.Equal("OverallEffectiveness eq 1", request.Filter);
        Assert.Equal("full", request.QueryType);
        Assert.True(request.IncludeTotalCount); 
        Assert.Equal("URN,EstablishmentName,Postcode", request.Select);
    }

    [Fact]
    public async Task SearchEstablishment_IssuesTheExpectedRequestToTheEstablishmentIndex()
    {
        // Arrange
        AzureSearchApi.RespondWith(EstablishmentIndex, totalCount: 1,
            ApiData(2.0, ("URN", "123456"), ("EstablishmentName", "Greenfield Primary")));
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            "search_establishment",
            new Dictionary<string, object?>
            {
                ["query"] = "Greenfield",
                ["top"] = 5,
                ["filter"] = "TypeOfEstablishment eq 'Academy'"
            },
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);
        Assert.Equal(1, ResponseModelOf(result).TotalCount);

        var request = AzureSearchApi.SingleRequest;
        Assert.Equal(EstablishmentIndex, request.IndexName);
        Assert.Equal("Greenfield", request.Search);
        Assert.Equal(5, request.Top);
        Assert.Equal("TypeOfEstablishment eq 'Academy'", request.Filter);
        Assert.Null(request.Select);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, 50)]
    public async Task SearchOfsted_ClampsTopBetweenOneAndFifty(int requestedTop, int expectedTop)
    {
        // Arrange
        AzureSearchApi.RespondWith(OfstedIndex, totalCount: 0);
        var client = await CreateMcpClientAsync();

        // Action
        await client.CallToolAsync(
            "search_ofsted",
            new Dictionary<string, object?> { ["query"] = "*", ["top"] = requestedTop },
            cancellationToken: CancellationToken);

        // Assert
        Assert.Equal(expectedTop, AzureSearchApi.SingleRequest.Top);
    }

    [Fact]
    public async Task SearchOfsted_UsesDefaultTopOfTen_WhenNotSupplied()
    {
        // Arrange
        AzureSearchApi.RespondWith(OfstedIndex, totalCount: 0); 
        var client = await CreateMcpClientAsync();

        // Action
        await client.CallToolAsync(
            "search_ofsted",
            new Dictionary<string, object?> { ["query"] = "*" },
            cancellationToken: CancellationToken);

        // Assert
        Assert.Equal(10, AzureSearchApi.SingleRequest.Top);
    }

    [Fact]
    public async Task SearchOfsted_ReturnsEmptyResults_WhenTheIndexHasNoMatches()
    {
        // Arrange
        AzureSearchApi.RespondWith(OfstedIndex, totalCount: 0); 
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            "search_ofsted",
            new Dictionary<string, object?> { ["query"] = "nothing-matches-this" },
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var response = ResponseModelOf(result);
        Assert.Equal(0, response.TotalCount);
        Assert.Empty(ResultsArrayOf(response));
    }

    [Fact]
    public async Task SearchOfsted_SurfacesSearchApiFailuresInTheResponsePayload()
    {
        // Arrange
        AzureSearchApi.RespondWithError(OfstedIndex, HttpStatusCode.BadRequest, "Invalid OData filter expression.");
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            "search_ofsted",
            new Dictionary<string, object?> { ["query"] = "*", ["filter"] = "not a filter" },
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var response = ResponseModelOf(result);
        Assert.NotNull(response.Error);
        Assert.Contains("Invalid OData filter expression.", response.Error);
    }

    [Fact]
    public async Task SearchTools_AreHiddenAndRejected_WithoutToolsReadScope()
    {
        // Arrange
        var client = await CreateMcpClientAsync(scopes: [McpScope.ReadPrompts]);

        // Action
        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken);

        // Assert
        Assert.DoesNotContain(tools, tool => tool.Name == "search_ofsted");
        Assert.DoesNotContain(tools, tool => tool.Name == "search_establishment");

        var exception = await Assert.ThrowsAsync<McpProtocolException>(() => client.CallToolAsync(
            "search_ofsted",
            new Dictionary<string, object?> { ["query"] = "*" },
            cancellationToken: CancellationToken).AsTask()); 
        Assert.Contains("authorization", exception.Message, StringComparison.OrdinalIgnoreCase); 
        Assert.Empty(AzureSearchApi.Requests);
    }
}
