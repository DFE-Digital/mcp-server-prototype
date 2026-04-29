using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Web.Extensions; 

var builder = WebApplication.CreateBuilder(args);
 
builder.Services.AddMcpInfrastructure(builder.Configuration);    
 
//Build & configure middleware pipeline
var app = builder.Build();

var mcpServerConfiguration = app.Services.GetRequiredService<McpServerConfiguration>();

if (app.Environment.IsDevelopment())
{
    app.UseCors(InfrastructureConfiguration.DevelopmentCoresPolicyName); 
} 

app.MapHealthChecks(mcpServerConfiguration.HealthCheckEndpoint);

app.AddSecurity();

//app.UseApiKeyAuthentication();

app.MapMcp(mcpServerConfiguration.Endpoint);

app.MapGet("/", () => Results.Ok(new
{
    name = mcpServerConfiguration.Name,
    title = mcpServerConfiguration.Title,
    version = mcpServerConfiguration.Version,
    mcp = mcpServerConfiguration.Endpoint,
    description = mcpServerConfiguration.Description,
    health = mcpServerConfiguration.HealthCheckEndpoint
})); 

await app.RunAsync();