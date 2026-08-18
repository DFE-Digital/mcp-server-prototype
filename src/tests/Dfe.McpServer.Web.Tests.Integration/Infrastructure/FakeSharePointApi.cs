using GovUK.Dfe.CoreLibs.SharePoint.Interfaces;
using GovUK.Dfe.CoreLibs.SharePoint.Models;
using System.Text;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;

/// <summary>
/// Stands in for SharePoint at the client-library boundary.
///
/// The real <see cref="Dfe.Mcp.Server.Application.Services.SharePointDocumentService"/> runs against
/// this, so folder path construction, the Financial Health Assessment content type filter, the
/// year filtering and ordering, and the download and JSON shaping are all genuinely exercised.
/// </summary>
public sealed class FakeSharePointApi : ISharePointService
{
    private readonly Dictionary<string, List<SharePointFileInfo>> _filesByFolder = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, byte[]> _downloadsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _unreadablePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _listedFolders = [];
    private readonly List<(string ParentPath, string FileName)> _downloadedFiles = [];

    /// <summary>Every folder path the service listed, in order.</summary>
    public IReadOnlyList<string> ListedFolders => _listedFolders;

    /// <summary>Every file the service downloaded, in order.</summary>
    public IReadOnlyList<(string ParentPath, string FileName)> DownloadedFiles => _downloadedFiles;

    /// <summary>Places <paramref name="files"/> in <paramref name="folderPath"/>.</summary>
    public FakeSharePointApi WithFiles(string folderPath, params SharePointFileInfo[] files)
    {
        _filesByFolder[folderPath] = [.. files];

        foreach (var file in files)
            _downloadsByPath[DownloadKey(file.ParentPath ?? folderPath, file.Name!)] = Encoding.UTF8.GetBytes($"contents of {file.Name}");

        return this;
    }

    /// <summary>Makes a download of the named file fail to produce content.</summary>
    public FakeSharePointApi WithUnreadableFile(string parentPath, string fileName)
    {
        _unreadablePaths.Add(DownloadKey(parentPath, fileName));
        return this;
    }

    /// <summary>Builds a Financial Health Assessment document last modified in the given year.</summary>
    public static SharePointFileInfo FinancialHealthAssessment(string name, string parentPath, int year, int month = 6) =>
        File(name, parentPath, "FHA", year, month);

    /// <summary>Builds a document of an arbitrary content type.</summary>
    public static SharePointFileInfo File(string name, string parentPath, string contentType, int year, int month = 6) => new()
    {
        Id = $"{name}-{year}-{month}",
        Name = name,
        ParentPath = parentPath,
        ContentType = contentType,
        LastModified = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero),
        CreatedDateTime = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero),
        Size = 1024,
        WebUrl = $"https://fake.sharepoint.com{parentPath}/{name}"
    };

    public Task<IReadOnlyList<SharePointFileInfo>> ListFilesAsync(string parentPath, CancellationToken cancellationToken = default)
    {
        _listedFolders.Add(parentPath);

        return Task.FromResult<IReadOnlyList<SharePointFileInfo>>(
            _filesByFolder.TryGetValue(parentPath, out var files) ? files : []);
    }

    public Task<Stream> DownloadFileAsync(string parentPath, string fileName, CancellationToken cancellationToken = default)
    {
        _downloadedFiles.Add((parentPath, fileName));

        var key = DownloadKey(parentPath, fileName);

        if (_unreadablePaths.Contains(key) || !_downloadsByPath.TryGetValue(key, out var content))
            return Task.FromResult<Stream>(null!);

        return Task.FromResult<Stream>(new MemoryStream(content));
    }

    public Task CreateFolderAsync(string parentPath, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UploadFileAsync(string parentPath, string fileName, Stream fileStream, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteFileAsync(string parentPath, string fileName, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    private static string DownloadKey(string parentPath, string fileName) => $"{parentPath.TrimEnd('/')}/{fileName}";
}
