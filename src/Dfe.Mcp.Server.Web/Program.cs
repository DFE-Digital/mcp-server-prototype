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
    app.UseCors(InfrastructureConfiguration.CorsPolicyName); 
} 

app.MapHealthChecks(mcpServerConfiguration.HealthCheckEndpoint);

app.AddSecurityLayer();

app.SetServerConfiguration(builder.Configuration, mcpServerConfiguration);

await app.RunAsync();