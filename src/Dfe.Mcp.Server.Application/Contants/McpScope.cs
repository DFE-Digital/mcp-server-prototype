namespace Dfe.Mcp.Server.Application.Contants;

public static class Claim
{
    public const string ScopeName = "scp";
    public const string ScopeUrl = "http://schemas.microsoft.com/identity/claims/scope";
    public const string RoleName = "roles";
}
public static class McpRole
{
    public const string ReadTools = "Read.Tools";
    public const string ReadResource = "Read.Resources";
    public const string ReadPrompts = "Read.Prompts";
    public const string BriefingTool = "Briefing.Tool";
}

public static class McpScope
{
    public const string ReadTools = "Tools.Read";
    public const string ReadResource = "Resources.Read";
    public const string ReadPrompts = "Prompts.Read";
}

public static class Policy
{
    public const string ToolsAccess = "ToolsAccess";
    public const string ResourceAccess = "ResourceAccess";
    public const string PromptAccess = "PromptAccess";
}
