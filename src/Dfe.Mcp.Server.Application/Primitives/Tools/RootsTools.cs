using Dfe.Mcp.Server.Application.Services.Interfaces;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Primitives.Tools;

[McpServerToolType]
public class RepoTools(IRepositoryRetrieverService repositoryRetrieverService)
{
    [McpServerTool(Name = "read_file")]
    [Description("Reads a UTF-8 text file from the repository (restricted to REPO_ROOT).")]
    public async Task<string> ReadFile(
        [Description("Path relative to repo root, e.g. src/Domain/Order.cs")] string path)
    {
        var fullPath = repositoryRetrieverService.Resolve(path);

        if (!File.Exists(fullPath))
            return $"File not found: {path}";

        // Keep it safe: cap large files (tune this for your needs)
        const int maxChars = 120_000;

        var text = await File.ReadAllTextAsync(fullPath);
        if (text.Length > maxChars)
            text = text[..maxChars] + "\n\n[truncated]\n";

        return text;
    }

    [McpServerTool, Description("Lists files and directories under a repository path (restricted to REPO_ROOT).")]
    public Task<string[]> ListDir(
        [Description("Path relative to repo root, e.g. src/ or docs/")] string path = ".")
    {
        var fullPath = repositoryRetrieverService.Resolve(path);

        if (!Directory.Exists(fullPath))
            return Task.FromResult(new[] { $"Directory not found: {path}" });

        var entries = Directory.EnumerateFileSystemEntries(fullPath)
            .Select(p => Path.GetFileName(p))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(500) // safety cap
            .ToArray();

        return Task.FromResult(entries);
    }

    [McpServerTool(Name = "search_code")]
    [Description("Searches for a string in text files within the repository (restricted to REPO_ROOT).")]
    public async Task<string> SearchCode(
        [Description("Search query, e.g. 'IOrderService' or 'public class Order'")] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Query must not be empty.";

        var repoRoot = repositoryRetrieverService.Resolve(".");
        var results = new List<string>();
        var maxMatches = 200;
         
        foreach (var file in Directory.EnumerateFiles(repoRoot, "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}"))
                continue;
             
            var ext = Path.GetExtension(file);
            if (ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".pdf" or ".zip" or ".dll" or ".exe")
                continue;

            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(file);
            }
            catch
            {
                continue; // ignore unreadable/binary files
            }

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    var rel = Path.GetRelativePath(repoRoot, file);
                    results.Add($"{rel}:{i + 1}: {lines[i].Trim()}");

                    if (results.Count >= maxMatches)
                        goto Done;
                }
            }
        }

    Done:
        return results.Count == 0
            ? "No matches."
            : string.Join("\n", results);
    }
} 
