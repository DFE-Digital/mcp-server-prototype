using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Dfe.Mcp.Server.Application.Primitives.Tools;

[McpServerToolType]
public class RepoTools(IFileRetrieverService repositoryRetrieverService)
{
    [McpServerTool(Name = "read_file"), Description("Reads a file from restricted server location.")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<string> ReadFile(
        [Description("Path relative to repo root, e.g. src/Domain/file.txt")] string path)
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

    [McpServerTool(Name ="List_directory",Title = "List directory and files")]
    [Description("Lists files and directories under a restricted server path.")]
    [Authorize(Policy = Policy.ToolsAccess)]
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
    [Description("Searches for a string in text files within the restricted server location.")]
    [Authorize(Policy = Policy.ToolsAccess)]
    public async Task<string> SearchCode(
        [Description("Search query, e.g. 'Ofsted'")] string query)
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
