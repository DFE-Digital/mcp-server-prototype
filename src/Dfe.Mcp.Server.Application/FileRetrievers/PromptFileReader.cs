using Dfe.Mcp.Server.Application.FileRetrievers.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dfe.Mcp.Server.Application.FileRetrievers;

public class PromptFileReader(ILogger<PromptFileReader> logger) : IPromptFileReader
{
    public string Read(string path)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(fullPath))
        {
            logger.LogError("Prompt file not found: {FullPath}", fullPath);
            throw new FileNotFoundException("Prompt file not found", fullPath);
        }

        var content = File.ReadAllText(fullPath);

        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogError("Prompt file is empty: {FullPath}", fullPath);
            throw new InvalidOperationException($"Prompt file is empty: {fullPath}");
        }

        return content;
    }
}