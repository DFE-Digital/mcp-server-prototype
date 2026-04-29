using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dfe.Mcp.Server.Web.Tests;

public class AzureSearchServiceTests
{
    private static AzureSearchService BuildService(string endpoint, string apiKey)
    {
        var options = new AzureSearchConfiguration
        {
            Endpoint = endpoint,
            ApiKey = apiKey,
            Indexes = new Dictionary<string, string>
            {
                ["Ofsted"] = "ofstedindex",
                ["Establishment"] = "establishmentindex"
            },
            DefaultTop = 5
        };

        return new AzureSearchService(options, NullLogger<AzureSearchService>.Instance);
    }

    [Fact(Skip = "Requires live Azure AI Search resource")]
    public async Task SearchOfsted_ReturnsResults()
    {
        var svc = BuildService(
            Environment.GetEnvironmentVariable("AzureSearch__Endpoint")!,
            Environment.GetEnvironmentVariable("AzureSearch__ApiKey")!);

        var result = await svc.SearchOfstedAsync("outstanding", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result.Error);
        Assert.NotEmpty(result.Results);
    }

    [Fact(Skip = "Requires live Azure AI Search resource")]
    public async Task SearchEstablishment_WithFilter_ReturnsFilteredResults()
    {
        var svc = BuildService(
            Environment.GetEnvironmentVariable("AzureSearch__Endpoint")!,
            Environment.GetEnvironmentVariable("AzureSearch__ApiKey")!);

        var result = await svc.SearchEstablishmentAsync("*", filter: "TypeOfEstablishment eq 'Academy'", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SearchAsync_UnknownIndexKey_ReturnsError()
    {
        var svc = BuildService("https://dummy.search.windows.net", "dummykey");

        var result = await svc.SearchAsync("nonexistent", "test", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result.Error);
        Assert.Contains("not configured", result.Error);
    }
}
