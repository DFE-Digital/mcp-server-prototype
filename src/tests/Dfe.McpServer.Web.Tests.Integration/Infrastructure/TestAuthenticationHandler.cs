using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AppClaim = Dfe.Mcp.Server.Application.Contants.Claim;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;

/// <summary>
/// Header names understood by <see cref="TestAuthenticationHandler"/>.
/// </summary>
public static class TestAuthHeaders
{
    /// <summary>Presence of this header authenticates the request; the value becomes the user name.</summary>
    public const string Auth = "X-Test-Auth";

    /// <summary>
    /// Comma separated list of scope values. These are emitted the way Entra ID issues them: a
    /// single space delimited <c>scp</c> claim, not one claim per scope.
    /// </summary>
    public const string Scopes = "X-Test-Scopes";

    /// <summary>
    /// Comma separated list of role values. These are emitted the way Entra ID issues them: one
    /// <c>roles</c> claim per role.
    /// </summary>
    public const string Roles = "X-Test-Roles";
}

/// <summary>
/// Replaces the JWT bearer handler so integration tests can drive the real authorization
/// policies (<c>ToolsAccess</c>, <c>ResourceAccess</c>, <c>PromptAccess</c>) without a token issuer.
/// A request is only authenticated when it carries <see cref="TestAuthHeaders.Auth"/>, which keeps
/// the unauthenticated behaviour of the MCP endpoint testable.
/// </summary>
public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "IntegrationTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TestAuthHeaders.Auth, out var userName))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userName.ToString()),
            new(ClaimTypes.Name, userName.ToString())
        };

        // Entra ID packs every consented scope into one space delimited claim, so the tests must see
        // that shape rather than the easier one claim per scope form.
        var scopes = SplitHeader(TestAuthHeaders.Scopes);
        if (scopes.Length > 0)
            claims.Add(new Claim(AppClaim.ScopeName, string.Join(' ', scopes)));

        // Roles really are issued as one claim each.
        claims.AddRange(SplitHeader(TestAuthHeaders.Roles).Select(role => new Claim(AppClaim.RoleName, role)));

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, AppClaim.RoleName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string[] SplitHeader(string headerName) =>
        Request.Headers.TryGetValue(headerName, out var value)
            ? value.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
}
