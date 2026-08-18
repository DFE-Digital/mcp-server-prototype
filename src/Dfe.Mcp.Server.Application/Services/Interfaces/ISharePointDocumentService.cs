namespace Dfe.Mcp.Server.Application.Services.Interfaces;

public interface ISharePointDocumentService
{
    Task<string> GetFinancialHealthAssessmentAsync(string trustName, string trReference, CancellationToken cancellationToken);
}
