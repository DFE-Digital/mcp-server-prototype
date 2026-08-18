using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Primitives.Tools;

public class AzureAISearchElicitationToolsStatelessTests : McpIntegrationTestBase
{
    protected override bool? Stateless => true;

    [Fact]
    public async Task ElicitSearchOfsted_ReturnsNotSupported_WhenTheServerCannotElicit()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync("elicit_search_ofsted", cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);
        Assert.Equal(ErrorMessages.OfstedInteractiveInputNotSupported, ResponseModelOf(result).Error);
        Assert.Empty(AzureSearchApi.Requests);
    }

    [Fact]
    public async Task ElicitSearchEstablishment_ReturnsNotSupported_WhenTheServerCannotElicit()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync("elicit_search_establishment", cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);
        Assert.Equal(ErrorMessages.EstablishmentInteractiveInputNotSupported, ResponseModelOf(result).Error);
        Assert.Empty(AzureSearchApi.Requests);
    }
}
