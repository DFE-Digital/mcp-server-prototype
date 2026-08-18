namespace Dfe.Mcp.Server.Application.Domain;

internal sealed class TrustFolderResolver
{
    public static string GetTierFolder(string trustName)
    {
        if (string.IsNullOrWhiteSpace(trustName))
        {
            throw new ArgumentException(
                "Trust name is required.",
                nameof(trustName));
        }

        var firstCharacter = char.ToUpperInvariant(
            trustName.Trim()[0]);

        return firstCharacter switch
        {
            >= 'A' and <= 'C' => "Tier_51",
            >= 'D' and <= 'H' => "Tier_52",
            >= 'I' and <= 'O' => "Tier_53",
            >= 'P' and <= 'S' => "Tier_54",
            _ => "Tier_55"
        };
    }
}
