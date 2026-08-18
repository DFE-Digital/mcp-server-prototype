using Dfe.Mcp.Server.Application.Configurations;
using Dfe.Mcp.Server.Application.Enums;
using Dfe.Mcp.Server.Application.Extensions;
using Dfe.Mcp.Server.Application.FileRetrievers.Interfaces;
using Dfe.Mcp.Server.Application.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Dfe.Mcp.Server.Application.Tests.Services;

public class PromptRetrieverServiceTests
{
    private readonly IPromptFileReader _fileReader;
    private readonly ILogger<PromptRetrieverService> _logger;
    private readonly PromptConfiguration _configuration;

    public PromptRetrieverServiceTests()
    {
        _fileReader = Substitute.For<IPromptFileReader>();
        _logger = Substitute.For<ILogger<PromptRetrieverService>>();
        _configuration = new PromptConfiguration();
    }

    private PromptRetrieverService CreateService() => new(_fileReader, _configuration, _logger);

    [Fact]
    public void GetSystemPrompt_WhenPathConfiguredAndFileReadSucceeds_ReturnsFileContent()
    {
        // Arrange
        const string path = "prompts/mcp_governance.txt";
        const string expectedContent = "System prompt file content";
        _configuration.SystemPrompts[SystemPromptType.McpGovernance] = path;
        _fileReader.Read(path).Returns(expectedContent);

        var promptRetrieverService = CreateService();

        // Action
        var result = promptRetrieverService.GetSystemPrompt(SystemPromptType.McpGovernance);

        // Assert
        Assert.Equal(expectedContent, result);
        _fileReader.Received(1).Read(path);
        AssertNoWarningLogged();
    }

    [Fact]
    public void GetSystemPrompt_WhenTypeNotConfigured_ReturnsFallbackAndLogsWarning()
    {
        // Arrange
        var promptRetrieverService = CreateService();

        // Action
        var result = promptRetrieverService.GetSystemPrompt(SystemPromptType.McpGovernance);

        // Assert
        Assert.Equal(SystemPromptType.McpGovernance.GetDescription(), result);
        _fileReader.DidNotReceive().Read(Arg.Any<string>());
        AssertSingleWarningLogged();
    }

    [Fact]
    public void GetSystemPrompt_WhenFileReaderThrows_ReturnsFallbackAndLogsWarningWithException()
    {
        // Arrange
        const string path = "prompts/mcp_governance.txt";
        var thrown = new IOException("disk error");
        _configuration.SystemPrompts[SystemPromptType.McpGovernance] = path;
        _fileReader.Read(path).Throws(thrown);

        var promptRetrieverService = CreateService();

        // Action
        var result = promptRetrieverService.GetSystemPrompt(SystemPromptType.McpGovernance);

        // Assert
        Assert.Equal(SystemPromptType.McpGovernance.GetDescription(), result);
        AssertSingleWarningLogged(thrown);
    }

    [Fact]
    public void GetSystemPrompt_WhenUnknownTypeAndNotConfigured_ReturnsGenericFallback()
    {
        // Arrange
        var promptRetrieverService = CreateService();

        // Action
        var result = promptRetrieverService.GetSystemPrompt((SystemPromptType)999);

        // Assert
        Assert.StartsWith("You are an AI assistant operating in a UK education environment.", result);
        Assert.Contains("Treat MCP tool availability as capability, not permission.", result);
    }

    /// <summary>
    /// Asserts exactly one warning was logged, optionally carrying <paramref name="expectedException"/>.
    /// </summary>
    private void AssertSingleWarningLogged(Exception? expectedException = null)
    {
        var warning = Assert.Single(LogEntries(), entry => entry.Level == LogLevel.Warning);

        if (expectedException is not null)
            Assert.Same(expectedException, warning.Exception);
    }

    private void AssertNoWarningLogged() =>
        Assert.DoesNotContain(LogEntries(), entry => entry.Level == LogLevel.Warning);

    /// <summary>
    /// Reads the calls made to <see cref="ILogger.Log{TState}"/>. The state type is an internal
    /// framework struct, so the arguments are inspected positionally rather than matched by type.
    /// </summary>
    private (LogLevel Level, Exception? Exception)[] LogEntries() =>
        [.. _logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => call.GetArguments())
            .Select(arguments => ((LogLevel)arguments[0]!, (Exception?)arguments[3]))];
}
