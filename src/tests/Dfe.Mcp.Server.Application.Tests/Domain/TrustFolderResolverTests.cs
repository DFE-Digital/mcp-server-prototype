using Dfe.Mcp.Server.Application.Domain;
using Xunit;

namespace Dfe.Mcp.Server.Application.Tests.Domain;

public class TrustFolderResolverTests
{
    [Theory]
    // Tier_51 covers A-C, including both boundaries.
    [InlineData("Academy Trust", "Tier_51")]
    [InlineData("Bright Futures Trust", "Tier_51")]
    [InlineData("Cedarwood Trust", "Tier_51")]
    // Tier_52 covers D-H.
    [InlineData("Dales Trust", "Tier_52")]
    [InlineData("Example Trust", "Tier_52")]
    [InlineData("Harbour Trust", "Tier_52")]
    // Tier_53 covers I-O.
    [InlineData("Ivy Trust", "Tier_53")]
    [InlineData("Northern Education Trust", "Tier_53")]
    [InlineData("Oakfield Trust", "Tier_53")]
    // Tier_54 covers P-S.
    [InlineData("Pioneer Trust", "Tier_54")]
    [InlineData("Riverside Trust", "Tier_54")]
    [InlineData("Summit Trust", "Tier_54")]
    // Tier_55 is the catch-all for T-Z.
    [InlineData("Thames Trust", "Tier_55")]
    [InlineData("Zenith Trust", "Tier_55")]
    public void GetTierFolder_MapsTheFirstLetterToItsTier(string trustName, string expectedTier)
    {
        // Arrange & Action
        var result = TrustFolderResolver.GetTierFolder(trustName);

        // Assert
        Assert.Equal(expectedTier, result);
    }

    [Theory]
    [InlineData("academy trust", "Tier_51")]
    [InlineData("northern education trust", "Tier_53")]
    public void GetTierFolder_IsCaseInsensitive(string trustName, string expectedTier)
    {
        // Arrange & Action
        var result = TrustFolderResolver.GetTierFolder(trustName);

        // Assert
        Assert.Equal(expectedTier, result);
    }

    [Fact]
    public void GetTierFolder_IgnoresLeadingWhitespace()
    {
        // Arrange & Action
        var result = TrustFolderResolver.GetTierFolder("   Example Trust");

        // Assert
        Assert.Equal("Tier_52", result);
    }

    [Theory] 
    [InlineData("1st Choice Trust")]
    [InlineData("#Hashtag Trust")]
    [InlineData("Ätna Trust")]
    public void GetTierFolder_FallsBackToTheCatchAllTier_ForNonAlphabeticFirstCharacters(string trustName)
    {
        // Arrange & Action
        var result = TrustFolderResolver.GetTierFolder(trustName);

        // Assert
        Assert.Equal("Tier_55", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetTierFolder_Throws_WhenTrustNameIsMissing(string? trustName)
    {
        // Arrange & Action
        var exception = Assert.Throws<ArgumentException>(() => TrustFolderResolver.GetTierFolder(trustName!));

        // Assert
        Assert.Equal("trustName", exception.ParamName);
        Assert.Contains("Trust name is required.", exception.Message);
    }
}
