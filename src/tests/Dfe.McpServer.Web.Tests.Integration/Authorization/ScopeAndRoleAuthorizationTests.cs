using Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using AppClaim = Dfe.Mcp.Server.Application.Contants.Claim;
using AppPolicy = Dfe.Mcp.Server.Application.Contants.Policy;
using McpRole = Dfe.Mcp.Server.Application.Contants.McpRole;
using McpScope = Dfe.Mcp.Server.Application.Contants.McpScope;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Authorization;

public class ScopeAndRoleAuthorizationTests : McpIntegrationTestBase
{
    private static readonly (string Policy, string Scope, string Role)[] PolicyMatrix =
    [
        (AppPolicy.ToolsAccess, McpScope.ReadTools, McpRole.ReadTools),
        (AppPolicy.ResourceAccess, McpScope.ReadResource, McpRole.ReadResource),
        (AppPolicy.PromptAccess, McpScope.ReadPrompts, McpRole.ReadPrompts)
    ];

    private IAuthorizationService AuthorizationService =>
        Factory.Services.GetRequiredService<IAuthorizationService>();

    private Task<bool> AuthorizeAsync(ClaimsPrincipal user, string policy) =>
        AuthorizationService.AuthorizeAsync(user, resource: null, policy).ContinueWith(t => t.Result.Succeeded);

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role));

    /// <summary>A token carrying all three scopes in one space delimited claim, as Entra ID issues it.</summary>
    private static Claim AllScopesClaim(string claimType) =>
        new(claimType, string.Join(' ', McpScope.ReadTools, McpScope.ReadResource, McpScope.ReadPrompts));

    [Theory]
    [InlineData(AppClaim.ScopeName)]
    [InlineData(AppClaim.ScopeUrl)]
    public async Task EveryPolicy_IsSatisfied_ByATokenCarryingAllScopesInOneSpaceDelimitedClaim(string scopeClaimType)
    {
        // Arrange
        var user = Principal(AllScopesClaim(scopeClaimType));

        // Action & Assert
        foreach (var (policy, _, _) in PolicyMatrix)
            Assert.True(await AuthorizeAsync(user, policy), $"{policy} rejected a token holding every scope.");
    }

    [Theory]
    [InlineData(AppClaim.ScopeName)]
    [InlineData(AppClaim.ScopeUrl)]
    public async Task EachPolicy_IsSatisfied_ByATokenCarryingOnlyItsOwnScope(string scopeClaimType)
    {
        // Arrange & Action & Assert
        foreach (var (policy, scope, _) in PolicyMatrix)
        {
            var user = Principal(new Claim(scopeClaimType, scope));

            Assert.True(await AuthorizeAsync(user, policy), $"{policy} rejected a token holding only {scope}.");
        }
    }

    [Fact]
    public async Task APolicy_IsNotSatisfied_ByAScopeBelongingToAnotherPolicy()
    {
        // Arrange
        // The scope values share a vocabulary, so a naive substring match would wrongly succeed here.
        var user = Principal(new Claim(AppClaim.ScopeName, McpScope.ReadTools));

        // Action & Assert
        Assert.True(await AuthorizeAsync(user, AppPolicy.ToolsAccess));
        Assert.False(await AuthorizeAsync(user, AppPolicy.ResourceAccess));
        Assert.False(await AuthorizeAsync(user, AppPolicy.PromptAccess));
    }

    [Fact]
    public async Task EveryPolicy_IsSatisfied_ByATokenCarryingRolesAsSeparateClaims()
    {
        // Arrange
        var user = Principal(
            new Claim(AppClaim.RoleName, McpRole.ReadTools),
            new Claim(AppClaim.RoleName, McpRole.ReadResource),
            new Claim(AppClaim.RoleName, McpRole.ReadPrompts));

        // Action & Assert
        foreach (var (policy, _, _) in PolicyMatrix)
            Assert.True(await AuthorizeAsync(user, policy), $"{policy} rejected a token holding every role.");
    }

    [Fact]
    public async Task EveryPolicy_IsSatisfied_WhenInboundClaimMappingRenamesRolesToTheLongUri()
    {
        // Arrange
        // JwtBearer runs with MapInboundClaims enabled, so "roles" reaches the policy as ClaimTypes.Role.
        var user = Principal(
            new Claim(ClaimTypes.Role, McpRole.ReadTools),
            new Claim(ClaimTypes.Role, McpRole.ReadResource),
            new Claim(ClaimTypes.Role, McpRole.ReadPrompts));

        // Action & Assert
        foreach (var (policy, _, _) in PolicyMatrix)
            Assert.True(await AuthorizeAsync(user, policy), $"{policy} rejected mapped role claims.");
    }

    [Fact]
    public async Task NoPolicy_IsSatisfied_ByATokenWithNeitherScopesNorRoles()
    {
        // Arrange
        var user = Principal(new Claim(ClaimTypes.Name, "someone"));

        // Action & Assert
        foreach (var (policy, _, _) in PolicyMatrix)
            Assert.False(await AuthorizeAsync(user, policy), $"{policy} accepted a token with no scopes or roles.");
    }

    [Fact]
    public async Task NoPolicy_IsSatisfied_ByAnUnrelatedScope()
    {
        // Arrange
        var user = Principal(new Claim(AppClaim.ScopeName, "User.Read Mail.Send"));

        // Action & Assert
        foreach (var (policy, _, _) in PolicyMatrix)
            Assert.False(await AuthorizeAsync(user, policy), $"{policy} accepted an unrelated scope.");
    }

}
