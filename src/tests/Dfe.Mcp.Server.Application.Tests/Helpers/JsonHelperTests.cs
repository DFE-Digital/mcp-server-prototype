using Dfe.Mcp.Server.Application.Helpers;
using Xunit;

namespace Dfe.Mcp.Server.Application.Tests.Helpers;

public class JsonHelperTests
{
    private sealed record Establishment(int Urn, string? SchoolName, string[]? Phases = null);

    [Fact]
    public void Serialize_WritesIndentedJsonWithPascalCasePropertyNames()
    {
        // Arrange
        var establishment = new Establishment(100000, "Alpha School");

        // Action
        var json = JsonHelper.Serialize(establishment);

        // Assert
        Assert.Contains("\"Urn\": 100000", json);
        Assert.Contains("\"SchoolName\": \"Alpha School\"", json);
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public void Serialize_WritesNullsRatherThanOmittingThem()
    {
        // Arrange & Action
        var json = JsonHelper.Serialize(new Establishment(100000, null));

        // Assert
        Assert.Contains("\"SchoolName\": null", json);
    }

    [Fact]
    public void Serialize_HandlesNull()
    {
        // Arrange & Action
        var json = JsonHelper.Serialize<Establishment?>(null);

        // Assert
        Assert.Equal("null", json);
    }

    [Fact]
    public void Serialize_HandlesCollections()
    {
        // Arrange & Action
        var json = JsonHelper.Serialize(new[] { new Establishment(1, "A"), new Establishment(2, "B") });

        // Assert
        Assert.StartsWith("[", json);
        Assert.EndsWith("]", json);
        Assert.Contains("\"Urn\": 1", json);
        Assert.Contains("\"Urn\": 2", json);
    }

    [Fact]
    public void Deserialize_ReadsPropertiesRegardlessOfCasing()
    {
        // Arrange
        const string json = """{"urn":123456,"schoolname":"Greenfield Primary"}""";

        // Action
        var result = JsonHelper.Deserialize<Establishment>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123456, result.Urn);
        Assert.Equal("Greenfield Primary", result.SchoolName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deserialize_ReturnsDefault_WhenJsonIsMissingOrBlank(string? json)
    {
        // Arrange & Action
        var result = JsonHelper.Deserialize<Establishment>(json!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_ReturnsDefaultForValueTypes_WhenJsonIsBlank()
    {
        // Arrange & Action
        var result = JsonHelper.Deserialize<int>("  ");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void SerializeThenDeserialize_RoundTripsTheValue()
    {
        // Arrange
        var original = new Establishment(100001, "Brambleton Secondary", ["Primary", "Secondary"]);

        // Action
        var result = JsonHelper.Deserialize<Establishment>(JsonHelper.Serialize(original));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original.Urn, result.Urn);
        Assert.Equal(original.SchoolName, result.SchoolName);
        Assert.Equal(original.Phases, result.Phases);
    }

    [Fact]
    public void Deserialize_Throws_ForMalformedJson()
    {
        // Arrange & Action & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => JsonHelper.Deserialize<Establishment>("{ not json"));
    }
}
