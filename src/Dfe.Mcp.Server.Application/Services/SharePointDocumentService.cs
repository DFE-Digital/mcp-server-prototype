using Dfe.Mcp.Server.Application.Domain;
using Dfe.Mcp.Server.Application.Helpers;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using GovUK.Dfe.CoreLibs.SharePoint.Interfaces;
using GovUK.Dfe.CoreLibs.SharePoint.Models;
using GovUK.Dfe.CoreLibs.SharePoint.Settings;

namespace Dfe.Mcp.Server.Application.Services;

public sealed class SharePointDocumentService(IDateTimeService dateTimeService, ISharePointService sharePointService, SharePointOptions sharePointOptions) : ISharePointDocumentService
{
    private const string FinancialHealthAssessmentContentType = "FHA";  

    public async Task<string> GetFinancialHealthAssessmentAsync(string trustName, string trReference, CancellationToken cancellationToken = default)
    {
        var folderPath = BuildFHAFolderPath(trustName, trReference);
        var latestFile = await FindLatestFhaAsync(folderPath, cancellationToken); 

        if (latestFile is null)
        {
            return JsonHelper.Serialize(new
            {
                error =
                    $"No Financial Health Assessment document was found for trust '{trustName}' " +
                    $"with reference '{trReference}'."
            });
        }

        using var file = await sharePointService.DownloadFileAsync(latestFile.ParentPath!, latestFile.Name, cancellationToken);

        if (file is null)
        {
            return JsonHelper.Serialize(new
            {
                error =
                    $"The latest Financial Health Assessment document '{latestFile.Name}' " +
                    "could not be retrieved from SharePoint."
            });
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        return JsonHelper.Serialize(new
        {
            latestFile.Name,
            latestFile.ParentPath,
            latestFile.WebUrl,
            latestFile.ContentType,
            latestFile.LastModified,
            SizeInBytes = buffer.Length,
            ContentBase64 = Convert.ToBase64String(buffer.ToArray())
        });
    }

    private async Task<SharePointFileInfo?> FindLatestFhaAsync(string trustFolderPath, CancellationToken cancellationToken)
    {
        var SFSOFolderPath = $"{trustFolderPath.TrimEnd('/')}/SFSO/";

        var trustRootFiles = await sharePointService.ListFilesAsync(trustFolderPath, cancellationToken);

        var SFSOFolderFiles = await sharePointService.ListFilesAsync(SFSOFolderPath, cancellationToken);

        var currentYear = dateTimeService.CurrentYear;

        return trustRootFiles
            .Concat(SFSOFolderFiles)
            .Where(IsFinancialHealthAssessment)
            .Where(file => file.LastModified.HasValue && file.LastModified.Value.Year <= currentYear)
            .OrderByDescending(file => file.LastModified!.Value.Year == currentYear)
            .ThenByDescending(file => file.LastModified)
            .FirstOrDefault();
    }

    private string BuildFHAFolderPath(string trustName,string trReference)
    {
        var tierFolder = TrustFolderResolver.GetTierFolder(trustName);

        var trustFolder = $"{trustName.Replace(" ", "*").ToLowerInvariant()}" +
            $"*{trReference.ToUpperInvariant()}";

        return $"{sharePointOptions.SitePath.TrimEnd('/')}{tierFolder}/{trustFolder}/";
    } 

    private static bool IsFinancialHealthAssessment(SharePointFileInfo file)
                => string.Equals( file.ContentType, 
                    FinancialHealthAssessmentContentType, 
                    StringComparison.OrdinalIgnoreCase);
}
