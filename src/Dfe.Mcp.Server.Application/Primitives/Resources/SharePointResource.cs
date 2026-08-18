using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Primitives.Resources;

[McpServerResourceType]
public class SharePointResource(ISharePointDocumentService sharePointDocumentService)
{
    [McpServerResource( UriTemplate = "SharePoint://FinancialHealthAssessment/{trReference}/{trustName}",
        Name = "get_latest_financial_health_assessment", Title = "Get Latest Financial Health Assessment", MimeType = "application/json")]
    [Description("Retrieves the latest Financial Health Assessment document for the specified trust. ")]
    [Authorize(Policy = Policy.ResourceAccess)]
    public async Task<string> GetLatestFinancialHealthAssessmentAsync(
    [Description("The trust reference number.")] string trReference,
    [Description("The name of the trust.")] string trustName, 
    CancellationToken cancellationToken = default)
    {
        return await sharePointDocumentService.GetFinancialHealthAssessmentAsync(trustName, trReference, cancellationToken);
    }
}
