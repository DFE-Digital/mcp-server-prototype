using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Contants;

namespace Dfe.Mcp.Server.Web.Middleware;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, McpServerConfiguration mcpServerOptions)
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly McpServerConfiguration _mcpServerOptions = mcpServerOptions;
    private readonly string _apiKey = configuration["MCP_API_KEY"]
            ?? throw new InvalidOperationException("MCP_API_KEY is not configured.");

    public async Task InvokeAsync(HttpContext context)
    {
        //Exclude health check endpoint
        if (context.Request.Path == _mcpServerOptions.HealthCheckEndpoint)
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var key) ||
            key.ToString() != _apiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "x-api-key";
            context.Response.ContentType = "application/json";

            var error = new
            {
                error = "unauthorized",
                message = ErrorMessages.InvaliKeyMessage,
                statusCode = context.Response.StatusCode,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsJsonAsync(error);
            return;
        }

        await next(context);
    }
}
