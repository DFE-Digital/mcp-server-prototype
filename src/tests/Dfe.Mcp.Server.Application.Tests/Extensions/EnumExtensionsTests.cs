using Dfe.Mcp.Server.Application.Enums;
using Dfe.Mcp.Server.Application.Extensions;
using System.ComponentModel;
using Xunit;

namespace Dfe.Mcp.Server.Application.Tests.Extensions;

public class EnumExtensionsTests
{
    [Fact]
    public void GetDescription_ReturnsTheGovernanceTextForTheSystemPromptType()
    {
        // Arrange & Action
        var result = SystemPromptType.McpGovernance.GetDescription();

        // Assert
        Assert.Equal("MCP Governance and Safe Tool Use", result);
    }

    [Fact]
    public void EverySystemPromptType_HasANonEmptyDescriptionToFallBackOn()
    {
        // Arrange
        var promptTypes = Enum.GetValues<SystemPromptType>();

        // Action & Assert
        Assert.NotEmpty(promptTypes);

        foreach (var promptType in promptTypes)
        {
            var description = promptType.GetDescription();

            Assert.False(
                string.IsNullOrWhiteSpace(description),
                $"{promptType} has no description to fall back on when its prompt file is unavailable.");

            Assert.NotEqual(promptType.ToString(), description);
        }
    }
}
