using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Text;
using System.Text.Json;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Primitives.Resources;
 
public class SharePointResourceTests : McpIntegrationTestBase
{
    private const string ResourceName = "get_latest_financial_health_assessment";
    private const string UriTemplate = "SharePoint://FinancialHealthAssessment/{trReference}/{trustName}";
    private const string TrustName = "Example Trust";
    private const string TrustReference = "TR12345";
    private const string TrustFolder = "/sites/fake-trustsTier_52/example*trust*TR12345/"; 
    private const string SfsoFolder = TrustFolder + "SFSO/"; 
    private static int CurrentYear => DateTimeOffset.Now.Year;

    [Fact]
    public async Task ListResourceTemplates_ExposesTheFinancialHealthAssessmentTemplate()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: CancellationToken);

        // Assert
        var template = Assert.Single(templates, resource => resource.Name == ResourceName);
        Assert.Equal(UriTemplate, template.UriTemplate);
        Assert.Equal("Get Latest Financial Health Assessment", template.Title);
        Assert.Equal("application/json", template.MimeType);
    }

    [Fact]
    public async Task ReadResource_LooksInTheTrustFolderAndItsSfsoSubfolder()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        await ReadAsync(client);

        // Assert
        Assert.Equal([TrustFolder, SfsoFolder], SharePointApi.ListedFolders);
    }

    [Fact]
    public async Task ReadResource_DerivesTheTierFolderFromTheTrustName()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        await ReadAsync(client, trustName: "Northern Education Trust", trustReference: "tr98765");

        // Assert
        Assert.Equal("/sites/fake-trustsTier_53/northern*education*trust*TR98765/", SharePointApi.ListedFolders[0]);
    }

    [Fact]
    public async Task ReadResource_ReturnsNotFound_WhenTheTrustHasNoDocuments()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var payload = await ReadJsonAsync(client);

        // Assert
        Assert.Contains("No Financial Health Assessment document was found for trust 'Example Trust' with reference 'TR12345'.",
            payload.GetProperty("error").GetString());

        Assert.Empty(SharePointApi.DownloadedFiles);
    }

    [Fact]
    public async Task ReadResource_IgnoresDocumentsThatAreNotFinancialHealthAssessments()
    {
        // Arrange
        SharePointApi.WithFiles(TrustFolder, FakeSharePointApi.File("Budget Forecast.xlsx", TrustFolder, contentType: "BFR", CurrentYear),
            FakeSharePointApi.File("Governance Review.docx", TrustFolder, contentType: "Document", CurrentYear));
        var client = await CreateMcpClientAsync();

        // Action
        var payload = await ReadJsonAsync(client);

        // Assert
        Assert.Contains("No Financial Health Assessment document was found", payload.GetProperty("error").GetString());
        Assert.Empty(SharePointApi.DownloadedFiles);
    }

    [Fact]
    public async Task ReadResource_IgnoresDocumentsDatedAfterTheCurrentYear()
    {
        // Arrange
        SharePointApi.WithFiles(TrustFolder, FakeSharePointApi.FinancialHealthAssessment("FHA Future.pdf", TrustFolder, CurrentYear + 1));
        var client = await CreateMcpClientAsync();

        // Action
        var payload = await ReadJsonAsync(client);
        
        // Assert
        Assert.Contains("No Financial Health Assessment document was found", payload.GetProperty("error").GetString());
        Assert.Empty(SharePointApi.DownloadedFiles);
    }

    [Fact]
    public async Task ReadResource_DownloadsTheMostRecentAssessmentAcrossBothFolders()
    {
        SharePointApi.WithFiles(TrustFolder,FakeSharePointApi.FinancialHealthAssessment("FHA 2 years ago.pdf", TrustFolder, CurrentYear - 2))
            .WithFiles(SfsoFolder,FakeSharePointApi.FinancialHealthAssessment("FHA last year.pdf", SfsoFolder, CurrentYear - 1),
                FakeSharePointApi.FinancialHealthAssessment("FHA this year January.pdf", SfsoFolder, CurrentYear, month: 1),
                FakeSharePointApi.FinancialHealthAssessment("FHA this year June.pdf", SfsoFolder, CurrentYear, month: 6));
        var client = await CreateMcpClientAsync();

        // Action
        await ReadAsync(client);

        // Assert
        var (ParentPath, FileName) = Assert.Single(SharePointApi.DownloadedFiles);
        Assert.Equal(SfsoFolder, ParentPath);
        Assert.Equal("FHA this year June.pdf", FileName);
    }

    [Fact]
    public async Task ReadResource_ReturnsTheDocumentMetadataAndContent()
    {
        // Arrange
        SharePointApi.WithFiles(TrustFolder, FakeSharePointApi.FinancialHealthAssessment("FHA current.pdf", TrustFolder, CurrentYear));
        var client = await CreateMcpClientAsync();

        // Action
        var payload = await ReadJsonAsync(client);

        // Assert
        Assert.Equal("FHA current.pdf", payload.GetProperty("Name").GetString());
        Assert.Equal(TrustFolder, payload.GetProperty("ParentPath").GetString());
        Assert.Equal("FHA", payload.GetProperty("ContentType").GetString());

        var content = Encoding.UTF8.GetString(Convert.FromBase64String(payload.GetProperty("ContentBase64").GetString()!));
        Assert.Equal("contents of FHA current.pdf", content);
        Assert.Equal(content.Length, payload.GetProperty("SizeInBytes").GetInt64());
    }

    [Fact]
    public async Task ReadResource_PrefersTheCurrentYearOverAnOlderButLaterDatedAssessment()
    {
        // Arrange
        SharePointApi.WithFiles(TrustFolder,
            FakeSharePointApi.FinancialHealthAssessment("FHA last year December.pdf", TrustFolder, CurrentYear - 1, month: 12),
            FakeSharePointApi.FinancialHealthAssessment("FHA this year January.pdf", TrustFolder, CurrentYear, month: 1));
        var client = await CreateMcpClientAsync();

        // Action
        await ReadAsync(client);

        // Assert
        Assert.Equal("FHA this year January.pdf", Assert.Single(SharePointApi.DownloadedFiles).FileName);
    }

    [Fact]
    public async Task ReadResource_ReturnsRetrievalError_WhenTheDocumentCannotBeDownloaded()
    {
        // Arrange
        SharePointApi
            .WithFiles(TrustFolder, FakeSharePointApi.FinancialHealthAssessment("FHA current.pdf", TrustFolder, CurrentYear))
            .WithUnreadableFile(TrustFolder, "FHA current.pdf");
        var client = await CreateMcpClientAsync();

        // Action
        var payload = await ReadJsonAsync(client);

        // Assert
        Assert.Contains("The latest Financial Health Assessment document 'FHA current.pdf' could not be retrieved from SharePoint.",
            payload.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ReadResource_FailsForAnUnknownUri()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync();

        // Assert
        await Assert.ThrowsAsync<McpProtocolException>(() => client.ReadResourceAsync(
            "SharePoint://SomethingElse/TR12345", cancellationToken: CancellationToken).AsTask());
    }

    [Fact]
    public async Task Resource_IsHiddenAndRejected_WithoutResourcesReadScope()
    {
        // Arrange
        var client = await CreateMcpClientAsync(scopes: [McpScope.ReadTools]);

        // Action
        var templates = await client.ListResourceTemplatesAsync(cancellationToken: CancellationToken);
        
        // Assert
        Assert.DoesNotContain(templates, resource => resource.Name == ResourceName);
        await Assert.ThrowsAsync<McpProtocolException>(() => ReadAsync(client));
        Assert.Empty(SharePointApi.ListedFolders);
    }


    private static Task<ReadResourceResult> ReadAsync(ModelContextProtocol.Client.McpClient client,
        string trustName = TrustName, string trustReference = TrustReference) =>
        client.ReadResourceAsync( $"SharePoint://FinancialHealthAssessment/{trustReference}/{trustName}",
            cancellationToken: CancellationToken).AsTask();

    private static async Task<JsonElement> ReadJsonAsync(ModelContextProtocol.Client.McpClient client,
        string trustName = TrustName, string trustReference = TrustReference)
    {
        var result = await ReadAsync(client, trustName, trustReference);
        var contents = Assert.IsType<TextResourceContents>(Assert.Single(result.Contents));

        return JsonDocument.Parse(contents.Text!).RootElement.Clone();
    }
}
