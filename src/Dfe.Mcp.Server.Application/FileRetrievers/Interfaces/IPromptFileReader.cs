namespace Dfe.Mcp.Server.Application.FileRetrievers.Interfaces;

public interface IPromptFileReader
{
    string Read(string path);
}
public interface IPromptRetriever
{
    string GetPrompt();
}
