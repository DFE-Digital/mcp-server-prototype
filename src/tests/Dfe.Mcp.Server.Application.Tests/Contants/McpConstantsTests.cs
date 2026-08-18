using Dfe.Mcp.Server.Application.Contants;
using Xunit;

namespace Dfe.Mcp.Server.Application.Tests.Contants;

public class McpConstantsTests
{
    [Fact]
    public void Claim_MatchesTheClaimTypesAzureAdIssues()
    {
        // Arrange & Action & Assert
        Assert.Equal("scp", Claim.ScopeName);
        Assert.Equal("http://schemas.microsoft.com/identity/claims/scope", Claim.ScopeUrl);
        Assert.Equal("roles", Claim.RoleName);
    }

    [Fact]
    public void McpScope_MatchesTheScopesExposedByTheAppRegistration()
    {
        // Arrange & Action & Assert
        Assert.Equal("Tools.Read", McpScope.ReadTools);
        Assert.Equal("Resources.Read", McpScope.ReadResource);
        Assert.Equal("Prompts.Read", McpScope.ReadPrompts);
    }

    [Fact]
    public void McpRole_MatchesTheAppRolesGrantedInAzureAd()
    {
        // Arrange & Action & Assert
        Assert.Equal("Read.Tools", McpRole.ReadTools);
        Assert.Equal("Read.Resources", McpRole.ReadResource);
        Assert.Equal("Read.Prompts", McpRole.ReadPrompts);
        Assert.Equal("Briefing.Tool", McpRole.BriefingTool);
    }

    [Fact]
    public void QueryState_MatchesTheStatesTheDatabricksStatementApiReturns()
    {
        // Arrange & Action & Assert
        Assert.Equal("SUCCEEDED", QueryState.Succeeded);
        Assert.Equal("FAILED", QueryState.Failed);
        Assert.Equal("CANCELED", QueryState.Canceled);
        Assert.Equal("CLOSED", QueryState.Closed);
        Assert.Equal("PENDING", QueryState.Pending);
        Assert.Equal("RUNNING", QueryState.Running);
    }


    [Fact]
    public void ErrorMessages_ForUnsupportedElicitation_PointAtTheDirectSearchTool()
    {
        Assert.Contains("search_ofsted", ErrorMessages.OfstedInteractiveInputNotSupported);
        Assert.Contains("search_establishment", ErrorMessages.EstablishmentInteractiveInputNotSupported);
    }
}
