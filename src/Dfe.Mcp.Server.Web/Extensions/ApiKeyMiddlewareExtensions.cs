using Dfe.Mcp.Server.Web.Middleware;

namespace Dfe.Mcp.Server.Web.Extensions;

public static class ApiKeyMiddlewareExtensions
{
    /// <summary>
    /// Add API key based authentication
    /// </summary>
    /// <param name="app"></param>
    /// <returns>An instance of <see cref="IApplicationBuilder"/></returns>
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyMiddleware>();
    }
}
