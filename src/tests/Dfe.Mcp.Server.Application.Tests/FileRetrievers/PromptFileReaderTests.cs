using Dfe.Mcp.Server.Application.FileRetrievers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Dfe.Mcp.Server.Application.Tests.FileRetrievers;

public sealed class PromptFileReaderTests : IDisposable
{
    private readonly ILogger<PromptFileReader> _logger = Substitute.For<ILogger<PromptFileReader>>();
    private readonly string _relativeFolder = $"prompt-file-reader-tests-{Guid.NewGuid():N}";

    private string AbsoluteFolder => Path.Combine(AppContext.BaseDirectory, _relativeFolder);

    public PromptFileReaderTests() => Directory.CreateDirectory(AbsoluteFolder);

    public void Dispose()
    {
        if (Directory.Exists(AbsoluteFolder))
            Directory.Delete(AbsoluteFolder, recursive: true);
    }

    private PromptFileReader CreateReader() => new(_logger);

    private string WriteFile(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(AbsoluteFolder, fileName), content);
        return $"{_relativeFolder}/{fileName}";
    }

    [Fact]
    public void Read_ReturnsTheFileContent_ForAPathRelativeToTheBaseDirectory()
    {
        // Arrange
        const string content = "## MCP GOVERNANCE\n\nUse tools carefully.";
        var relativePath = WriteFile("governance.md", content);

        // Action
        var result = CreateReader().Read(relativePath);

        // Assert
        Assert.Equal(content, result);
        AssertNoErrorLogged();
    }

    [Fact]
    public void Read_AcceptsPlatformSpecificSeparators()
    {
        // Arrange
        var relativePath = WriteFile("governance.md", "content");
        var windowsStylePath = relativePath.Replace('/', '\\');

        // Action
        var result = CreateReader().Read(windowsStylePath);

        // Assert
        Assert.Equal("content", result);
    }

    [Fact]
    public void Read_Throws_WhenTheFileDoesNotExist()
    {
        // Arrange
        var missingPath = $"{_relativeFolder}/does-not-exist.md";

        // Action
        var exception = Assert.Throws<FileNotFoundException>(() => CreateReader().Read(missingPath));

        // Assert
        Assert.Equal(Path.Combine(AppContext.BaseDirectory, missingPath), exception.FileName);
        AssertSingleErrorLogged();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void Read_Throws_WhenTheFileIsEmptyOrWhitespace(string content)
    {
        // Arrange
        var relativePath = WriteFile("blank.md", content);

        // Action
        var exception = Assert.Throws<InvalidOperationException>(() => CreateReader().Read(relativePath));

        // Assert
        Assert.Contains("Prompt file is empty", exception.Message);
        Assert.Contains("blank.md", exception.Message);
        AssertSingleErrorLogged();
    }

    private void AssertSingleErrorLogged() =>
        Assert.Single(LogEntries(), level => level == LogLevel.Error);

    private void AssertNoErrorLogged() =>
        Assert.DoesNotContain(LogEntries(), level => level == LogLevel.Error);

    private LogLevel[] LogEntries() =>
        [.. _logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => (LogLevel)call.GetArguments()[0]!)];
}
