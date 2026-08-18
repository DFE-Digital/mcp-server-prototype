using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Data.Models;
using Dfe.Mcp.Server.Domain;
using Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;
using ModelContextProtocol;
using System.Net;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Primitives.Tools;

public class DataQueryToolsTests : McpIntegrationTestBase
{
    private const string OfstedToolName = "query_establishments_with_ofsted_data";
    private const string DatabricksToolName = "query_establishments_data"; 
    private const string RowHeader = "urn|establishment_name|ukprn";

    protected override bool UseInMemoryAcademiesDatabase => true;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        await Factory.SeedEstablishmentsAsync(
            Establishment(100001, "Ashfield Primary", "Camden", "London", "Primary", "Good", "SW1A 1AA", "Yes", 250),
            Establishment(100002, "Brambleton Secondary", "Camden", "London", "Secondary", "Outstanding", "SW1A 2BB", "Yes", 1200),
            Establishment(100003, "Cedarwood Academy", "Islington", "London", "Primary", "Requires improvement", "N1 9GU", "No", 400),
            Establishment(100004, "Dalefield High", "Manchester", "North West", "Secondary", "Good", "M1 3BB", "Yes", 900));
    }

    #region Databricks backed tool

    [Fact]
    public async Task ListTools_ExposesDataQueryTools()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken);

        // Assert
        var databricksTool = Assert.Single(tools, tool => tool.Name == DatabricksToolName);
        Assert.Equal("Query establishment, school or academy", databricksTool.Title);

        var ofstedTool = Assert.Single(tools, tool => tool.Name == OfstedToolName);
        Assert.Equal("Query establishment, school or academy with Ofsted data", ofstedTool.Title);
    }

    [Fact]
    public async Task QueryEstablishmentsData_SubmitsTheStatementWithTheConfiguredWarehouseAndToken()
    {
        // Arrange
        DatabricksApi.RespondWithResult(rowCount: 0); 
        var client = await CreateMcpClientAsync();

        // Action
        await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?> { ["urn"] = "123456" },
            cancellationToken: CancellationToken);

        // Assert
        var submission = DatabricksApi.SingleSubmission;
        Assert.Equal("/api/2.0/sql/statements", submission.Path);
        Assert.Equal(McpServerWebApplicationFactory.FakeDatabricksWarehouseId, submission.WarehouseId);
        Assert.Equal(McpServerWebApplicationFactory.FakeDatabricksAccessToken, submission.BearerToken);
        Assert.Equal("0s", submission.WaitTimeout);
    }

    [Fact]
    public async Task QueryEstablishmentsData_BuildsSqlWithTheJoinAndLimit()
    {
        // Arrange
        DatabricksApi.RespondWithResult(rowCount: 0); 
        var client = await CreateMcpClientAsync();

        // Action
        await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?> { ["limit"] = 3 },
            cancellationToken: CancellationToken);

        // Assert
        var statement = DatabricksApi.SingleSubmission.NormalisedStatement;

        Assert.Contains("SELECT e.* FROM rsd_catalog_20_silver.manual_data_loading_area.dr_suess_establishment e", statement);
        Assert.Contains("INNER JOIN rsd_catalog_20_silver.manual_data_loading_area.dr_suess_trust t ON e.trust_name = t.trust_name", statement);
        Assert.EndsWith("LIMIT 3", statement);
        Assert.DoesNotContain("WHERE", statement);
    }

    [Fact]
    public async Task QueryEstablishmentsData_BuildsAWhereClauseFromEveryFilter()
    {
        // Arrange
        DatabricksApi.RespondWithResult(rowCount: 0); 
        var client = await CreateMcpClientAsync();

        // Action
        await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?>
            {
                ["urn"] = "123456",
                ["ukprn"] = "10012345",
                ["name"] = "Greenfield Primary",
                ["establishmentPhase"] = "Primary",
                ["TrustName"] = "Greenfield Trust",
                ["groupId"] = "TR01234"
            },
            cancellationToken: CancellationToken);

        // Assert
        var statement = DatabricksApi.SingleSubmission.NormalisedStatement;

        Assert.Contains("WHERE LOWER(e.group_id) = 'tr01234'", statement);
        Assert.Contains("AND e.urn = '123456'", statement);
        Assert.Contains("AND e.ukprn = '10012345'", statement);
        Assert.Contains("AND LOWER(e.establishment_name) = 'greenfield primary'", statement);
        Assert.Contains("AND LOWER(e.establishment_phase) = 'primary'", statement);
        Assert.Contains("AND LOWER(t.trust_name) = 'greenfield trust'", statement);
    }

    [Fact]
    public async Task QueryEstablishmentsData_EscapesSingleQuotesInFilterValues()
    {
        // Arrange
        DatabricksApi.RespondWithResult(rowCount: 0); 
        var client = await CreateMcpClientAsync();

        // Action
        await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?> { ["name"] = "St Mary's Academy" },
            cancellationToken: CancellationToken);

        // Assert
        Assert.Contains(
            "LOWER(e.establishment_name) = 'st mary''s academy'",
            DatabricksApi.SingleSubmission.NormalisedStatement);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(50, 10)]
    public async Task QueryEstablishmentsData_ClampsLimitBetweenOneAndTen(int requestedLimit, int expectedLimit)
    {
        // Arrange
        DatabricksApi.RespondWithResult(rowCount: 0); 
        var client = await CreateMcpClientAsync();

        // Action
        await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?> { ["limit"] = requestedLimit },
            cancellationToken: CancellationToken);

        // Asset
        Assert.EndsWith($"LIMIT {expectedLimit}", DatabricksApi.SingleSubmission.NormalisedStatement);
    }

    [Fact]
    public async Task QueryEstablishmentsData_ProjectsRowsThatCarryThePipeDelimitedHeader()
    {
        // Arrange
        DatabricksApi.RespondWithResult(
            rowCount: 2,
            [RowHeader, "Alpha School", "10012345"],
            [RowHeader, "Beta School", "10067890"]); 
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?> { ["establishmentPhase"] = "Primary" },
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var response = ResponseModelOf(result);
        Assert.Null(response.Error);
        Assert.Equal(2, response.TotalCount);

        var rows = ResultsArrayOf(response);
        Assert.Equal(2, rows.Length);
        Assert.Equal("Alpha School", rows[0].GetProperty("establishment_name").GetString());
        Assert.Equal("10012345", rows[0].GetProperty("ukprn").GetString());
        Assert.Equal("Beta School", rows[1].GetProperty("establishment_name").GetString());
    }

    [Fact]
    public async Task QueryEstablishmentsData_FailsToProjectAConventionalDatabricksPayload()
    {
        // Arrange
        DatabricksApi.RespondWithResult(
            rowCount: 2,
            ["100000", "Alpha School", "10012345"],
            ["100001", "Beta School", "10067890"]); 
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var response = ResponseModelOf(result);
        Assert.NotNull(response.Error);
        Assert.Null(response.Results);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task QueryEstablishmentsData_PollsUntilTheStatementCompletes()
    {
        // Arrange
        DatabricksApi
            .RespondWithPendingPolls(2)
            .RespondWithResult(rowCount: 1, [RowHeader, "Alpha School", "10012345"]); 
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);
        Assert.Equal(1, ResponseModelOf(result).TotalCount);

        // Two RUNNING polls followed by the SUCCEEDED poll.
        Assert.Equal(3, DatabricksApi.PollCount);
    }

    [Fact]
    public async Task QueryEstablishmentsData_ReturnsEmptyResults_WhenThePayloadHasNoResultSection()
    {
        // Arrange
        DatabricksApi.RespondWithRawResult("""{"statement_id":"x","status":{"state":"SUCCEEDED"}}"""); 
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken);

        // Assert
        var response = ResponseModelOf(result);
        Assert.Equal(0, response.TotalCount);
        Assert.Empty(ResultsArrayOf(response));
    }

    [Fact]
    public async Task QueryEstablishmentsData_SurfacesSubmitFailuresInTheResponsePayload()
    {
        // Arrange
        DatabricksApi.RespondWithSubmitError(HttpStatusCode.Forbidden, """{"message":"token expired"}"""); 
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?> { ["urn"] = "123456" },
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var response = ResponseModelOf(result);
        Assert.NotNull(response.Error);
        Assert.Contains("Databricks returned 403", response.Error);
        Assert.Contains("token expired", response.Error);
    }

    [Fact]
    public async Task QueryEstablishmentsData_SurfacesAFailedStatementInTheResponsePayload()
    {
        // Arrange
        DatabricksApi.RespondWithFailedStatement("Table or view not found: dr_suess_establishment"); 
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var response = ResponseModelOf(result);
        Assert.NotNull(response.Error);
        Assert.Contains("Query FAILED", response.Error);
        Assert.Contains("Table or view not found", response.Error);
    }

    [Fact]
    public async Task QueryEstablishmentsData_SurfacesPollFailuresInTheResponsePayload()
    {
        // Arrange
        DatabricksApi.RespondWithPollError(HttpStatusCode.InternalServerError, "workspace unavailable"); 
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken);

        // Assert
        var response = ResponseModelOf(result);
        Assert.NotNull(response.Error);
        Assert.Contains("Databricks API returned 500", response.Error);
    }

    #endregion

    #region Academies database backed tool

    [Fact]
    public async Task QueryEstablishmentsWithOfstedData_ReturnsAllSeededRows_WhenNoFilterIsSupplied()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            OfstedToolName,
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken);

        // Assert
        AssertToolSucceeded(result);

        var response = ResponseModelOf(result);
        Assert.Null(response.Error);
        Assert.Equal(4, response.TotalCount);
    }


    public static TheoryData<string, Dictionary<string, object?>, string[]> SingleFilterCases => new()
    {
        { "urn", new() { ["urn"] = 100003 }, ["Cedarwood Academy"] },
        { "postcode prefix", new() { ["postcode"] = "SW1A" }, ["Ashfield Primary", "Brambleton Secondary"] },
        { "pupil range", new() { ["minPupils"] = 300, ["maxPupils"] = 1000 }, ["Cedarwood Academy", "Dalefield High"] },
        { "overall effectiveness", new() { ["overallEffectiveness"] = "Good" }, ["Ashfield Primary", "Dalefield High"] }
    };

    [Theory]
    [MemberData(nameof(SingleFilterCases))]
    public async Task QueryEstablishmentsWithOfstedData_AppliesEachFilter(
        string filterName, // labels the case in the test output
        Dictionary<string, object?> arguments,
        string[] expectedSchoolNames)
    {
        _ = filterName;

        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(OfstedToolName, arguments, cancellationToken: CancellationToken);

        // Assert
        var response = ResponseModelOf(result);
        Assert.Equal(expectedSchoolNames, SchoolNames(response));
        Assert.Equal(expectedSchoolNames.Length, response.TotalCount);
    }

    [Fact]
    public async Task QueryEstablishmentsWithOfstedData_CombinesFiltersAndOrdersBySchoolName()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            OfstedToolName,
            new Dictionary<string, object?>
            {
                ["localAuthority"] = "Camden",
                ["ofstedRegion"] = "London",
                ["safeguardingIsEffective"] = true
            },
            cancellationToken: CancellationToken);

        // Assert
        var response = ResponseModelOf(result);
        Assert.Equal(["Ashfield Primary", "Brambleton Secondary"], SchoolNames(response));
    }

    [Fact]
    public async Task QueryEstablishmentsWithOfstedData_AppliesTheRequestedLimit()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            OfstedToolName,
            new Dictionary<string, object?> { ["limit"] = 2 },
            cancellationToken: CancellationToken);

        // Assert
        var response = ResponseModelOf(result);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(["Ashfield Primary", "Brambleton Secondary"], SchoolNames(response));
    }

    [Fact]
    public async Task QueryEstablishmentsWithOfstedData_ReturnsNoRows_WhenNothingMatches()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.CallToolAsync(
            OfstedToolName,
            new Dictionary<string, object?> { ["localAuthority"] = "Nowhere" },
            cancellationToken: CancellationToken);

        // Assert
        var response = ResponseModelOf(result);
        Assert.Equal(0, response.TotalCount);
        Assert.Empty(SchoolNames(response));
    }

    #endregion

    [Fact]
    public async Task DataQueryTools_AreHiddenAndRejected_WithoutToolsReadScope()
    {
        // Arrange
        var client = await CreateMcpClientAsync(scopes: [McpScope.ReadResource]);

        // Action
        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken);

        // Assert
        Assert.DoesNotContain(tools, tool => tool.Name == DatabricksToolName);
        Assert.DoesNotContain(tools, tool => tool.Name == OfstedToolName);

        await Assert.ThrowsAsync<McpProtocolException>(() => client.CallToolAsync(
            DatabricksToolName,
            new Dictionary<string, object?> { ["urn"] = "123456" },
            cancellationToken: CancellationToken).AsTask());

        Assert.Empty(DatabricksApi.Submissions);
    }

    private static string[] SchoolNames(ResponseModel response) =>
        [.. ResultsArrayOf(response).Select(element => element.GetProperty("SchoolName").GetString()!)];

    private static MisEstablishment Establishment(
        int urn,
        string schoolName,
        string localAuthority,
        string ofstedRegion,
        string ofstedPhase,
        string overallEffectiveness,
        string postcode,
        string safeguardingIsEffective,
        int totalNumberOfPupils) => new()
        {
            Urn = urn,
            SchoolName = schoolName,
            LocalAuthority = localAuthority,
            OfstedRegion = ofstedRegion,
            OfstedPhase = ofstedPhase,
            OverallEffectiveness = overallEffectiveness,
            Postcode = postcode,
            SafeguardingIsEffective = safeguardingIsEffective,
            TotalNumberOfPupils = totalNumberOfPupils
        };
}
