using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Contants;
using Dfe.Mcp.Server.Application.Enums;
using Dfe.Mcp.Server.Application.Extensions;
using Dfe.Mcp.Server.Web.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Dfe.Mcp.Server.Web.Tests.Integration.Primitives.Prompts;

public class PromptsTests : McpIntegrationTestBase
{
    private const string SystemPromptName = "get_system_prompt";
    private const string McpGovernancePromptPath = "Prompts/MCP_Governance_System_Prompt_Addendum.md";

    [Fact]
    public async Task ListPrompts_ExposesTheSystemPrompt()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var prompts = await client.ListPromptsAsync(cancellationToken: CancellationToken);

        // Assert
        var systemPrompt = Assert.Single(prompts, prompt => prompt.Name == SystemPromptName);
        Assert.Equal("Gets system instruction prompt", systemPrompt.Title);
        Assert.Equal("Gets a system instruction prompt.", systemPrompt.Description);
    }

    [Fact]
    public async Task ListPrompts_DeclaresTheRequiredPromptTypeArgument()
    {
        // Arrange
        var client = await CreateMcpClientAsync();

        // Action
        var prompts = await client.ListPromptsAsync(cancellationToken: CancellationToken);
        var prompt = Assert.Single(prompts, candidate => candidate.Name == SystemPromptName);

        // Assert
        var argument = Assert.Single(prompt.ProtocolPrompt.Arguments!);
        Assert.Equal("promptType", argument.Name);
        Assert.True(argument.Required);
    }

    [Fact]
    public void PromptConfiguration_BindsTheConfiguredMarkdownFile()
    {
        // Arrange & Action
        var configuration = Factory.Services.GetRequiredService<PromptConfiguration>();

        // Assert
        var path = Assert.Contains(SystemPromptType.McpGovernance, configuration.SystemPrompts);
        Assert.Equal(McpGovernancePromptPath, path);
    }

    [Fact]
    public async Task GetSystemPrompt_ReturnsTheMcpGovernanceMarkdownFile()
    {
        // Arrange
        var expected = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, McpGovernancePromptPath.Replace('/', Path.DirectorySeparatorChar)),
            CancellationToken);
        var client = await CreateMcpClientAsync();

        // Action
        var result = await client.GetPromptAsync(
            SystemPromptName,
            new Dictionary<string, object?> { ["promptType"] = nameof(SystemPromptType.McpGovernance) },
            cancellationToken: CancellationToken);

        // Assert
        var text = SingleMessageText(result); 
        Assert.Equal(expected, text); 
        Assert.NotEqual(SystemPromptType.McpGovernance.GetDescription(), text);
        Assert.Contains("Least privilege", text);
    }


    [Fact]
    public async Task GetPrompt_FailsForAnUnknownPromptType()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync();

        // Assert
        await Assert.ThrowsAsync<McpProtocolException>(() => client.GetPromptAsync(
            SystemPromptName,
            new Dictionary<string, object?> { ["promptType"] = "NotARealPromptType" },
            cancellationToken: CancellationToken).AsTask());
    }

    [Fact]
    public async Task GetPrompt_FailsWhenThePromptTypeArgumentIsMissing()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync();

        // Assert
        await Assert.ThrowsAsync<McpProtocolException>(() => client.GetPromptAsync(
            SystemPromptName,
            cancellationToken: CancellationToken).AsTask());
    }

    [Fact]
    public async Task GetPrompt_FailsForAnUnknownPromptName()
    {
        // Arrange & Action
        var client = await CreateMcpClientAsync();

        // Assert
        await Assert.ThrowsAsync<McpProtocolException>(() => client.GetPromptAsync(
            "NotARealPrompt",
            cancellationToken: CancellationToken).AsTask());
    }

    [Fact]
    public async Task Prompts_AreHiddenAndRejected_WithoutPromptsReadScope()
    {
        // Arrange
        var client = await CreateMcpClientAsync(scopes: [McpScope.ReadTools]);

        // Action
        var prompts = await client.ListPromptsAsync(cancellationToken: CancellationToken);

        // Assert
        Assert.DoesNotContain(prompts, prompt => prompt.Name == SystemPromptName); 
        await Assert.ThrowsAsync<McpProtocolException>(() => client.GetPromptAsync(SystemPromptName,
            new Dictionary<string, object?> { ["promptType"] = nameof(SystemPromptType.McpGovernance) },
            cancellationToken: CancellationToken).AsTask());
    }


    private static string SingleMessageText(GetPromptResult result)
    {
        var message = Assert.Single(result.Messages);
        var content = Assert.IsType<TextContentBlock>(message.Content);
        return content.Text;
    }
}
