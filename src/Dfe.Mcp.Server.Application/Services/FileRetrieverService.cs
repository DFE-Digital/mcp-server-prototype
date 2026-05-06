using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Dfe.Mcp.Server.Application.Services;

public class FileRetrieverService(RestrictedPathsConfiguration restrictedPathsConfiguration) : IFileRetrieverService
{
    public string Resolve(string userPath)
    {
        if (IsUrl(userPath))
            userPath = ExtractPathFromUrl(userPath);

        var repoRoot = Path.GetFullPath(restrictedPathsConfiguration.ServerPath);

        var combined = Path.GetFullPath(Path.Combine(repoRoot, userPath ?? string.Empty));
         
        if (!IsSubPathOf(combined, repoRoot))
            throw new InvalidOperationException("Access denied: path is outside REPO_ROOT.");

        var fileName = Path.GetFileName(combined);
        if (fileName.Equals(".env", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Access denied: sensitive file.");

        if (combined.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Access denied: .git directory.");

        return combined;
    }

    /// <summary>
    /// Detects whether the input looks like an HTTP/HTTPS URL.
    /// </summary>
    private static bool IsUrl(string input) =>
        !string.IsNullOrWhiteSpace(input) &&
        (input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
         input.StartsWith("http://", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Extracts a relative file/folder path from a GitHub or SharePoint URL.
    ///
    /// GitHub examples:
    ///   https://github.com/user/repo/blob/main/src/Domain/Order.cs  → src/Domain/Order.cs
    ///   https://github.com/user/repo/tree/main/src/Domain            → src/Domain
    ///   https://github.com/user/repo                                 → .
    ///
    /// SharePoint examples:
    ///   https://tenant.sharepoint.com/sites/MySite/Shared Documents/folder/file.cs  → folder/file.cs
    ///   https://tenant.sharepoint.com/:f:/r/sites/MySite/Shared%20Documents/folder  → folder
    /// </summary>
    private static string ExtractPathFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid URL: {url}");

        var host = uri.Host.ToLowerInvariant();

        if (host == "github.com")
            return ExtractGitHubPath(uri);

        if (host.EndsWith(".sharepoint.com"))
            return ExtractSharePointPath(uri);

        throw new NotSupportedException(
            $"URL host '{uri.Host}' is not supported. Only github.com and *.sharepoint.com URLs are accepted.");
    }

    private static string ExtractGitHubPath(Uri uri)
    {
        var segments = uri.Segments
            .Select(s => Uri.UnescapeDataString(s).Trim('/'))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray(); 

        const int pathStartAfterVerb = 4;

        if (segments.Length <= 2)
            return "."; // just https://github.com/user/repo

        var verb = segments.ElementAtOrDefault(2);
        bool hasVerb = verb is "blob" or "tree" or "raw";

        if (!hasVerb || segments.Length < pathStartAfterVerb)
            return ".";

        var pathParts = segments.Skip(pathStartAfterVerb).ToArray();
        return string.Join(Path.DirectorySeparatorChar.ToString(), pathParts);
    }

    private static string ExtractSharePointPath(Uri uri)
    {
        var rawPath = Uri.UnescapeDataString(uri.AbsolutePath);
         
        var shortLinkMatch = Regex.Match(rawPath,
            @"^/:[a-z]:/r/sites/[^/]+/(?:Shared%20Documents|Shared Documents|[^/]+)/(.+)$",
            RegexOptions.IgnoreCase);

        if (shortLinkMatch.Success)
            return ToLocalPath(shortLinkMatch.Groups[1].Value);
         
        var libraryMatch = Regex.Match(rawPath,
            @"/(?:Shared[ %]20Documents|Documents|[^/]+Library)/(.+)$",
            RegexOptions.IgnoreCase);

        if (libraryMatch.Success)
            return ToLocalPath(libraryMatch.Groups[1].Value);
         
        var sitesMatch = Regex.Match(rawPath, @"^/sites/[^/]+/(.*)$", RegexOptions.IgnoreCase);
        if (sitesMatch.Success)
            return ToLocalPath(sitesMatch.Groups[1].Value);

        throw new ArgumentException(
            $"Could not extract a file path from SharePoint URL: {uri}");
    }

    /// <summary>Converts forward-slash URL path to OS-appropriate separator.</summary>
    private static string ToLocalPath(string urlSubPath)
    {
        var decoded = Uri.UnescapeDataString(urlSubPath);
        return decoded.Replace('/', Path.DirectorySeparatorChar)
                      .Trim(Path.DirectorySeparatorChar);
    }
     
    private static bool IsSubPathOf(string candidate, string root)
    {
        root = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        candidate = candidate.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return candidate.StartsWith(root, comparison);
    }
}
